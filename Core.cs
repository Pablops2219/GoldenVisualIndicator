﻿using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[assembly: MelonInfo(typeof(GoldenVisualIndicator.Core), "GoldenVisualIndicator", "0.1", "pablops", null)]
[assembly: MelonGame("Bugfight Studio", "Beetleball")]

namespace GoldenVisualIndicator
{
    using HarmonyLib;

    //Parche de harmony, llama a la función OnDungBallSpawned cada vez que spawnea una bola
    [HarmonyPatch(typeof(DungBall), "OnNetworkSpawn")]
    public static class DungBall_OnNetworkSpawn_Patch
    {
        static void Postfix(DungBall __instance)
        {
            if (GoldenVisualIndicator.Core.Instance == null)
                return;

            GoldenVisualIndicator.Core.Instance.OnDungBallSpawned(__instance);
        }
    }

    public class Core : MelonMod
    {
        public string assetFolder;
        public static Core Instance;
        public override void OnInitializeMelon()
        {
            Instance = this;
            var harmony = new Harmony("golden.visual.indicator");
            harmony.PatchAll();


            string assemblyFolder = Path.GetDirectoryName(MelonAssembly.Assembly.Location);
            assetFolder = Path.Combine(assemblyFolder, "assets");

            LoggerInstance.Msg("GoldenVisualIndicator Mod Initialized.");
        }


        public List<DungBall> trackedDungBalls = new List<DungBall>();
        private HashSet<DungBall> goldenWarned = new HashSet<DungBall>();
        public override void OnUpdate()
        {
            if (onMainMenu) return;
            foreach (var db in trackedDungBalls)
            {
                if (db.Size >= 5 && !goldenWarned.Contains(db))
                {
                    goldenWarned.Add(db);
                    //LoggerInstance.Msg("Found a golden one!");
                    CreateGoldenWarningInHud();
                }
            }
        }

        public bool onMainMenu = true;
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            //LoggerInstance.Msg($"Scene {sceneName} with build index {buildIndex} has been loaded!");
            if (0 <= buildIndex && buildIndex <= 4)
            {
                onMainMenu = true;
                trackedDungBalls.Clear();
                goldenWarned.Clear();
                activeWarnings = 0;
            }
            else
            {
                onMainMenu = false;
            }
        }

        //Metodo al que llama el parche cuando spawnea una bola
        public void OnDungBallSpawned(DungBall db)
        {
            if (onMainMenu) return;

            if (db != null && !trackedDungBalls.Contains(db))
            {
                trackedDungBalls.Add(db);
            }
        }


        private RawImage goldenImage;
        private GameObject goldenGO = null;

        private int activeWarnings = 0;

        private void CreateGoldenWarningInHud()
        {
            GameObject go = new GameObject("GoldenWarn");
            if (hudRoot == null) FindHudRoot();
            go.transform.SetParent(hudRoot, false);

            var img = go.AddComponent<RawImage>();
            img.texture = LoadTexture("golden.png");

            RectTransform rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(100, 100);

            // Offset X por activeWarnings para que aparezcan uno detras de otro
            float xOffset = 80 + (activeWarnings * 50);
            rt.anchoredPosition = new Vector2(xOffset, -500);

            go.AddComponent<IconBouncer>();

            activeWarnings++;
            MelonCoroutines.Start(DestroyGoldenWarn(4.0f, go));
        }


        private IEnumerator DestroyGoldenWarn(float seconds, GameObject go)
        {
            yield return new WaitForSeconds(seconds);

            if (go != null)
            {
                activeWarnings = Mathf.Max(0, activeWarnings - 1);
                GameObject.Destroy(go);
            }
        }



        //No interesante-----------------------------------------------------------------
        public Transform hudRoot = null;
        private void FindHudRoot()
        {
            var canvases = GameObject.FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                if (canvas.name == "Canvas" || canvas.name == "Canvas_CustomLobby")
                {
                    Transform playerHud = canvas.transform.Find("PlayerHUD");
                    if (playerHud != null)
                    {
                        hudRoot = playerHud.Find("Root");
                        if (hudRoot != null)
                        {
                            //MelonLogger.Msg("HUD Root found");
                            return;
                        }
                    }
                }
            }
        }
        private Texture2D LoadTexture(string fileName)
        {
            string path = Path.Combine(assetFolder, fileName);

            if (!File.Exists(path))
            {
                MelonLogger.Error($"Texture not found: {path}");
                return null;
            }

            byte[] data = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(tex, data);
            return tex;
        }
    }

    //Rutina del icono para que rebote por su cuenta
    [RegisterTypeInIl2Cpp]
    public class IconBouncer : MonoBehaviour
    {
        public float speed = 3.0f;
        public float height = 45.0f;
        public float squashAmount = 0.15f;
        public Vector3 originalPos;
        private Vector3 originalScale = Vector3.one;

        void Start()
        {
            originalPos = transform.localPosition;
        }

        void Update()
        {
            UpdateBounce();
        }
        public void UpdateBounce()
        {
            // Calcular la funcion del rebote
            float rawSine = Mathf.Sin(Time.time * speed);
            float bounceValue = Mathf.Abs(rawSine);

            // Modificar la posición
            this.transform.localPosition = originalPos + new Vector3(0, bounceValue * height, 0);

            // Modificar la escala
            float squashFactor = Mathf.Lerp(squashAmount, -squashAmount, bounceValue);
            float scaleX = originalScale.x + squashFactor;
            float scaleY = originalScale.y - squashFactor;
            this.transform.localScale = new Vector3(scaleX, scaleY, originalScale.z);
        }

    }
}

