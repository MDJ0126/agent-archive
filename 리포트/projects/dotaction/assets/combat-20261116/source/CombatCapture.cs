using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TurnRoom.PixelCamera;

namespace DotAction.Editor
{
    /// <summary>
    /// 저장된 전투 씬을 실제 플레이하고 카메라 스택 출력과 동작 근거를 저장한다.
    /// </summary>
    public static class CombatCapture
    {
        private static float _started = -1;
        private static int _stage;
        private static int _errors;
        private static float _nextMove = 26;

        public static void Run()
        {
            EditorSceneManager.OpenScene(CombatSceneSetup.SCENE_PATH);
            SessionState.SetBool("DotAction.CombatCapture", true);
            EditorApplication.EnterPlaymode();
        }

        public static void RepairHudAndRun()
        {
            CombatSceneSetup.RepairHudLayer();
            Run();
        }

        public static void ConfigureViewAndRun()
        {
            CombatSceneSetup.ApplyDiabloView();
            Run();
        }

        public static void CaptureView()
        {
            SessionState.SetBool("DotAction.ShortCapture", true);
            ConfigureViewAndRun();
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (SessionState.GetBool("DotAction.CombatCapture", false))
            {
                Application.logMessageReceived += CountErrors;
                EditorApplication.update += Tick;
            }
        }

        private static void CountErrors(string condition, string trace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                _errors++;
            }
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            CombatSession session = UnityEngine.Object.FindFirstObjectByType<CombatSession>();
            if (session == null || session.player.Stats == null)
            {
                return;
            }
            if (_started < 0)
            {
                _started = Time.time;
            }
            float elapsed = Time.time - _started;
            try
            {
                if (_stage == 0 && elapsed > 2)
                {
                    bool accepted = session.SetDestination(new Vector2Int(-5, 2));
                    Debug.Log("DOTACTION_MOVE_REQUEST: (-5,2); accepted=" + accepted);
                    _stage++;
                }
                if (_stage == 1 && elapsed > 8)
                {
                    PixelCameraEffect effect = Camera.main.GetComponent<PixelCameraEffect>();
                    effect.enabled = false;
                    Capture("art/previews/combat-3d-before-pixels.png");
                    effect.enabled = true;
                    Capture("art/previews/combat-pixel-battle.png");
                    if (SessionState.GetBool("DotAction.ShortCapture", false))
                    {
                        SessionState.EraseBool("DotAction.ShortCapture");
                        SessionState.EraseBool("DotAction.CombatCapture");
                        EditorApplication.Exit(0);
                        return;
                    }
                    Debug.Log("DOTACTION_PIXEL_PATH: " + effect.LastRenderPath + "; captures=" + effect.LastSceneCaptures);
                    session.SetDestination(new Vector2Int(4, 4));
                    _stage++;
                }
                if (_stage == 2 && elapsed > 15)
                {
                    session.SetDestination(new Vector2Int(4, -4));
                    _stage++;
                }
                if (_stage == 3 && elapsed > 20)
                {
                    Capture("art/previews/combat-pixel-movement.png");
                    session.SetDestination(new Vector2Int(-5, -4));
                    _stage++;
                }
                if (elapsed >= _nextMove && !session.IsFinished)
                {
                    int index = Mathf.FloorToInt((_nextMove - 26) / 6) % 4;
                    Vector2Int[] destinations = { new(5, 2), new(-5, 3), new(-5, -4), new(4, -4) };
                    session.SetDestination(destinations[index]);
                    _nextMove += 6;
                }
                if (elapsed > 90 || session.IsFinished)
                {
                    Capture("art/previews/combat-pixel-final.png");
                    string evidence = $"elapsed={elapsed:F2}; steps={session.player.motor.CompletedSteps}; shots={session.player.ShotsFired}; kills={session.Kills}; wave={session.Wave}; health={session.player.Health}; finished={session.IsFinished}; errors={_errors}";
                    File.WriteAllText("art/previews/combat-play-evidence.txt", evidence);
                    Debug.Log("DOTACTION_PLAY_EVIDENCE: " + evidence);
                    SessionState.EraseBool("DotAction.CombatCapture");
                    EditorApplication.update -= Tick;
                    EditorApplication.Exit(_errors == 0 && session.player.motor.CompletedSteps > 0 && session.Kills > 0 ? 0 : 1);
                }
            }
            catch (Exception exception)
            {
                SessionState.EraseBool("DotAction.CombatCapture");
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Capture(string path)
        {
            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture(1280, 720, 24);
            RenderPipeline.SubmitRenderRequest(Camera.main, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("DOTACTION_COMBAT_CAPTURE: " + path);
        }
    }
}


