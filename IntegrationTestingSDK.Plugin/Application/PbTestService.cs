using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using IntegrationTestingSDK.Plugin.Infrastructure;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;

namespace IntegrationTestingSDK.Plugin.Application
{
    /// <summary>
    ///     Core service wrapping ModAPI for integration testing.
    ///     All game-thread work is enqueued via <see cref="MainThreadTask"/>.
    ///     Called by <see cref="HttpApiServer"/> — HTTP-friendly API, no direct IPC.
    /// </summary>
    internal sealed class PbTestService
    {
        private readonly ConcurrentQueue<MainThreadTask> _queue = new();
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
        private static readonly object _serializeLock = new();

        public bool IsReady => MySession.Static?.Ready == true;

        // ── IInGameHarness ─────────────────────────────────────

        public IReadOnlyList<long> SpawnTestGrid(string blueprintName, double x, double y, double z)
        {
            return Enqueue(() => DoSpawn(blueprintName, new Vector3D(x, y, z)));
        }

        public void RemoveTestGrid(long gridId)
        {
            EnqueueVoid(() => DoDelete(gridId));
        }

        public void SetScriptCode(long gridId, string code)
        {
            EnqueueVoid(() => DoUpload(gridId, code));
        }

        public string RunScript(long gridId, string argument = null)
        {
            return Enqueue(() => DoRun(gridId, argument ?? ""));
        }

        public string GetLcdContent(long gridId)
        {
            return Enqueue(() => DoGetLcd(gridId));
        }

        public IReadOnlyList<IntegrationTestingSDK.Contracts.BlockState> GetBlockStates(long gridId)
        {
            return Enqueue(() => DoGetBlockStates(gridId));
        }

        public void Dispose()
        {
            var tasks = new List<MainThreadTask>();
            while (_queue.TryDequeue(out var t)) tasks.Add(t);
            foreach (var t in tasks)
            {
                try { t.Done.Set(); } catch { }
                try { t.Dispose(); } catch { }
            }
        }

        // ── Main-thread processing ─────────────────────────────

        public void ProcessQueue()
        {
            while (_queue.TryDequeue(out var task))
            {
                try { task.Process?.Invoke(); }
                catch (Exception ex)
                {
                    Logger.Error("ProcessQueue", ex);
                    task.Error = ex;
                }
                finally { task.Done.Set(); }
            }
        }

        // ── Generic enqueue ────────────────────────────────────

        private T Enqueue<T>(Func<T> process)
        {
            using var task = new MainThreadTask<T>();
            task.Process = () => task.Result = process();
            _queue.Enqueue(task);
            if (!task.Done.Wait(_timeout))
            {
                Logger.Error($"Task timed out after {_timeout.TotalSeconds:N0}s");
                throw new TimeoutException($"MainThreadTask timed out after {_timeout.TotalSeconds:N0}s");
            }
            if (task.Error != null)
                throw task.Error;
            return task.Result;
        }

        private void EnqueueVoid(Action process)
        {
            using var task = new MainThreadTask();
            task.Process = process;
            _queue.Enqueue(task);
            if (!task.Done.Wait(_timeout))
            {
                Logger.Error($"Task timed out after {_timeout.TotalSeconds:N0}s");
                throw new TimeoutException($"MainThreadTask timed out after {_timeout.TotalSeconds:N0}s");
            }
            if (task.Error != null)
                throw task.Error;
        }

        // ── Game-thread implementations ────────────────────────

        private List<long> DoSpawn(string blueprintName, Vector3D position)
        {
            var bpPath = ResolveBlueprintPath(blueprintName);
            if (bpPath == null)
                throw new FileNotFoundException($"Blueprint not found: {blueprintName}");

            MyObjectBuilder_Definitions definitions;
            lock (_serializeLock)
            {
                using var stream = File.OpenRead(bpPath);
                VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen.DeserializeXML(
                    stream, out MyObjectBuilder_Base obj, typeof(MyObjectBuilder_Definitions));
                definitions = obj as MyObjectBuilder_Definitions;
            }

            if (definitions?.ShipBlueprints == null || definitions.ShipBlueprints.Length == 0)
                throw new InvalidOperationException("No ShipBlueprints in blueprint");

            var shipBp = definitions.ShipBlueprints[0];
            if (shipBp.CubeGrids == null || shipBp.CubeGrids.Length == 0)
                throw new InvalidOperationException("No CubeGrids in blueprint");

            var gridBuilders = shipBp.CubeGrids
                .OfType<MyObjectBuilder_CubeGrid>()
                .ToList();

            MyAPIGateway.Entities.RemapObjectBuilderCollection(gridBuilders);

            var gridIds = new List<long>();
            foreach (var gb in gridBuilders)
            {
                var po = gb.PositionAndOrientation;
                var m = po.HasValue
                    ? MatrixD.CreateWorld(po.Value.Position + position, po.Value.Forward, po.Value.Up)
                    : MatrixD.CreateWorld(position, Vector3D.Forward, Vector3D.Up);
                gb.PositionAndOrientation = new MyPositionAndOrientation(m);
                gb.CreatePhysics = true;
                gb.Editable = true;

                var ent = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(gb);
                if (ent is MyCubeGrid grid)
                {
                    gridIds.Add(grid.EntityId);
                    Logger.Info($"Spawned grid {grid.DisplayName} Id={grid.EntityId}");
                }
            }

            return gridIds;
        }

        private void DoDelete(long gridId)
        {
            var entity = MyAPIGateway.Entities.GetEntityById(gridId);
            if (entity == null)
                throw new InvalidOperationException($"Grid not found: {gridId}");

            MyAPIGateway.Entities.RemoveEntity(entity);
            Logger.Info($"Deleted grid {gridId}");
        }

        private void DoUpload(long gridId, string code)
        {
            var entity = MyAPIGateway.Entities.GetEntityById(gridId);
            if (!(entity is MyCubeGrid grid))
                throw new InvalidOperationException($"Grid not found: {gridId}");

            var modApiPb = FindFirstBlock<Sandbox.ModAPI.IMyProgrammableBlock>(grid);
            if (modApiPb == null)
                throw new InvalidOperationException($"Programmable block not found on grid {gridId}");

            modApiPb.ProgramData = code;
            Logger.Info($"Uploaded script to PB on grid {gridId} ({code.Length} chars)");
        }

        private string DoRun(long gridId, string argument)
        {
            var entity = MyAPIGateway.Entities.GetEntityById(gridId);
            if (!(entity is MyCubeGrid grid))
                throw new InvalidOperationException($"Grid not found: {gridId}");

            var ingamePb = FindFirstBlock<Sandbox.ModAPI.Ingame.IMyProgrammableBlock>(grid);
            if (ingamePb == null)
                throw new InvalidOperationException($"Programmable block not found on grid {gridId}");

            var ok = ingamePb.TryRun(argument);
            Logger.Info($"PB Run(\"{argument}\") on grid {gridId}: {(ok ? "OK" : "FAIL")}");

            if (!ok)
                throw new InvalidOperationException($"PB TryRun failed on grid {gridId}");

            // Don't read LCD here — caller uses GetLcdContent after a frame
            return "";
        }

        private string DoGetLcd(long gridId)
        {
            var entity = MyAPIGateway.Entities.GetEntityById(gridId);
            if (!(entity is MyCubeGrid grid))
                throw new InvalidOperationException($"Grid not found: {gridId}");

            return ReadLcdContent(grid) ?? "";
        }

        private List<IntegrationTestingSDK.Contracts.BlockState> DoGetBlockStates(long gridId)
        {
            var entity = MyAPIGateway.Entities.GetEntityById(gridId);
            if (!(entity is MyCubeGrid grid))
                throw new InvalidOperationException($"Grid not found: {gridId}");

            var result = new List<IntegrationTestingSDK.Contracts.BlockState>();
            foreach (var fat in grid.GetFatBlocks())
            {
                var slim = fat.BlockDefinition;
                var subtype = slim?.Id.SubtypeName;
                if (string.IsNullOrEmpty(subtype))
                    subtype = slim?.Id.SubtypeId.ToString();
                if (string.IsNullOrEmpty(subtype))
                    subtype = fat.GetType().Name;
                result.Add(new IntegrationTestingSDK.Contracts.BlockState
                {
                    EntityId = fat.EntityId,
                    Type = slim?.Id.TypeId.ToString() ?? "?",
                    Subtype = subtype,
                    Enabled = fat.IsWorking
                });
            }

            return result;
        }

        // ── Entity helpers ─────────────────────────────────────

        private static T FindFirstBlock<T>(MyCubeGrid grid) where T : class
        {
            foreach (var fat in grid.GetFatBlocks())
                if (fat is T typed)
                    return typed;
            return null;
        }

        private static string ReadLcdContent(MyCubeGrid grid)
        {
            // Find the first non-PB text panel with content.
            // PB also implements IMyTextSurfaceProvider, so we must skip it.
            foreach (var fat in grid.GetFatBlocks())
            {
                if (fat is Sandbox.ModAPI.IMyProgrammableBlock)
                    continue;

                if (fat is Sandbox.ModAPI.Ingame.IMyTextPanel panel)
                {
                    var text = panel.GetText();
                    if (!string.IsNullOrEmpty(text))
                        return text;
                }

                if (fat is Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider provider)
                {
                    var text = provider.GetSurface(0)?.GetText();
                    if (!string.IsNullOrEmpty(text))
                        return text;
                }
            }

            return null;
        }

        private static string ResolveBlueprintPath(string blueprintName)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var path = Path.Combine(appData, "SpaceEngineers", "Blueprints", "local",
                blueprintName, "bp.sbc");
            return File.Exists(path) ? path : null;
        }

        // ── MainThreadTask ─────────────────────────────────────

        private class MainThreadTask : IDisposable
        {
            public readonly ManualResetEventSlim Done = new(false);
            public Action Process;
            public Exception Error;
            public void Dispose() { Done?.Dispose(); }
        }

        private sealed class MainThreadTask<T> : MainThreadTask
        {
            public T Result;
        }
    }
}
