using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

namespace Fly
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        static ManualLogSource logger;
        public static ConfigEntry<KeyboardShortcut> toggleKey;

        private void Awake()
        {
            toggleKey = Config.Bind("General", "Toggle Flying Key", new KeyboardShortcut(KeyCode.F1), "Key to toggle flying");

            // Plugin startup logic
            logger = Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded");
            Logger.LogInfo($"Patching...");
            Harmony.CreateAndPatchAll(typeof(Plugin));
            Logger.LogInfo($"Patched");
        }

        [HarmonyPatch(typeof(PlayerManager), "Start")]
        [HarmonyPrefix]
        static void Start_Prefix()
        {
            new GameObject("__Fly__").AddComponent<FlyMod>().Init(toggleKey);
        }
    }

    public class FlyMod : MonoBehaviour
    {
        static ConfigEntry<KeyboardShortcut> toggleKey;

        public float cameraSensitivity = 90f;
        public float climbSpeed = 4f;
        public float normalMoveSpeed = 10f;
        public float slowMoveFactor = 0.25f;
        public float fastMoveFactor = 3f;
        public bool toggle;
        public Transform player;
        public Transform camera;
        private float rotationX;
        private float rotationY;

        public void Init(ConfigEntry<KeyboardShortcut> menuKey)
        {
            toggleKey = menuKey;
        }

        private void Start()
        {
            this.player = PlayerManager.Instance.PlayerTransform();
            this.camera = PlayerManager.Instance.fpsController.m_Camera.transform;
        }

        private void Update()
        {
            if (toggleKey.Value.IsDown())
            {
                this.toggle = !this.toggle;
                PlayerManager.Instance.SetKinematic(this.toggle);
                PlayerManager.Instance.SetLookEnabled(!this.toggle);
                PlayerManager.Instance.SetInputEnabled(!this.toggle);
                if (this.toggle)
                {
                    PlayerManager.Instance.Disable();
                    PlayerManager.Instance.DisableMovement();
                }
                else
                {
                    PlayerManager.Instance.Enable();
                    PlayerManager.Instance.EnableMovement();
                }
            }
            if (!this.toggle)
            {
                return;
            }
            PlayerManager.Instance.SetKinematic(this.toggle);
            if (this.toggle)
            {
                PlayerManager.Instance.Disable();
                PlayerManager.Instance.DisableMovement();
            }
            else
            {
                PlayerManager.Instance.Enable();
                PlayerManager.Instance.EnableMovement();
            }
            this.rotationX += Input.GetAxis("Mouse X") * this.cameraSensitivity * Time.deltaTime;
            this.rotationY += Input.GetAxis("Mouse Y") * this.cameraSensitivity * Time.deltaTime;
            this.rotationY = Mathf.Clamp(this.rotationY, -90f, 90f);
            this.player.localRotation = Quaternion.AngleAxis(this.rotationX, Vector3.up);
            this.player.localRotation *= Quaternion.AngleAxis(this.rotationY, Vector3.left);
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                this.player.position += this.camera.forward * (this.normalMoveSpeed * this.fastMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
                this.player.position += this.camera.right * (this.normalMoveSpeed * this.fastMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
            }
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                this.player.position += this.camera.forward * (this.normalMoveSpeed * this.slowMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
                this.player.position += this.camera.right * (this.normalMoveSpeed * this.slowMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
            }
            else
            {
                this.player.position += this.camera.forward * this.normalMoveSpeed * Input.GetAxis("Vertical") * Time.deltaTime;
                this.player.position += this.camera.right * this.normalMoveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                this.player.position -= this.camera.up * this.climbSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.E))
            {
                this.player.position += this.camera.up * this.climbSpeed * Time.deltaTime;
            }
            FirstPersonController instance = FirstPersonController.Instance;
            float num = Mathf.Clamp(this.player.position.y, instance.levelMinY, instance.levelMaxY);
            this.player.position = new Vector3(this.player.position.x, num, this.player.position.z);
        }
    }
}
