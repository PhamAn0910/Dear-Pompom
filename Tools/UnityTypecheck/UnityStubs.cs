// Hand-written, minimal stand-ins for the Unity APIs used by the scripts listed in
// UnityTypecheck.csproj. Signatures only; nothing here runs. See the csproj for why this exists.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static void DontDestroyOnLoad(Object target) { }
        public static void Destroy(Object target) { }
        public static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode) where T : Object { return null; }
        public static T FindFirstObjectByType<T>() where T : Object { return null; }
        public static implicit operator bool(Object o) { return o != null; }
        public static bool operator ==(Object a, Object b) { return ReferenceEquals(a, b); }
        public static bool operator !=(Object a, Object b) { return !ReferenceEquals(a, b); }
        public override bool Equals(object other) { return ReferenceEquals(this, other); }
        public override int GetHashCode() { return 0; }
    }
    public enum FindObjectsSortMode { None, InstanceID }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform;
        public T GetComponent<T>() { return default(T); }
        public T[] GetComponentsInChildren<T>(bool includeInactive = false) { return null; }
        public T GetComponentInParent<T>() { return default(T); }
    }
    public class Transform : Component { public void SetParent(Transform parent, bool worldPositionStays) { } }
    public class Behaviour : Component { public bool enabled; }
    public class Coroutine { }
    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator routine) { return null; }
        public void StopCoroutine(Coroutine routine) { }
        public void Invoke(string methodName, float time) { }
        public void CancelInvoke() { }
    }
    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public Transform transform;
        public SceneManagement.Scene scene;
        public bool activeSelf;
        public void SetActive(bool value) { }
        public T AddComponent<T>() where T : Component { return null; }
        public T GetComponent<T>() { return default(T); }
    }
    public class AudioClip : Object { }
    public class AudioSource : Behaviour
    {
        public AudioClip clip; public float volume; public bool mute, loop, playOnAwake; public float spatialBlend;
        public bool isPlaying { get { return false; } }
        public void Play() { } public void Stop() { } public void Pause() { } public void UnPause() { }
        public void PlayOneShot(AudioClip clip, float volumeScale = 1f) { }
    }
    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogError(object message) { }
        public static void LogException(Exception exception) { }
    }
    public static class Application { public static string persistentDataPath { get { return ""; } } }
    public static class JsonUtility
    {
        public static string ToJson(object obj, bool prettyPrint = false) { return ""; }
        public static T FromJson<T>(string json) { return default(T); }
    }
    public static class Time
    {
        public static float unscaledTime { get { return 0; } }
        public static float unscaledDeltaTime { get { return 0; } }
        public static float deltaTime { get { return 0; } }
        public static float time { get { return 0; } }
    }
    public static class Mathf
    {
        public static float Clamp01(float v) { return v; }
        public static float Lerp(float a, float b, float t) { return a; }
        public static float Max(float a, float b) { return a; }
        public static int Min(int a, int b) { return a; }
        public static int FloorToInt(float f) { return 0; }
    }
    public class WaitForSeconds { public WaitForSeconds(float seconds) { } }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, AfterAssembliesLoaded, BeforeSplashScreen, SubsystemRegistration }
    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { } }
    [AttributeUsage(AttributeTargets.Field)] public class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
    [AttributeUsage(AttributeTargets.Field)] public class HeaderAttribute : Attribute { public HeaderAttribute(string text) { } }
}

namespace UnityEngine.SceneManagement
{
    public enum LoadSceneMode { Single, Additive }
    public struct Scene
    {
        public string name { get { return ""; } }
        public static bool operator ==(Scene a, Scene b) { return true; }
        public static bool operator !=(Scene a, Scene b) { return false; }
        public override bool Equals(object other) { return true; }
        public override int GetHashCode() { return 0; }
    }
    public static class SceneManager
    {
        public static event Events.UnityAction<Scene, LoadSceneMode> sceneLoaded;
        public static event Events.UnityAction<Scene> sceneUnloaded;
        public static Scene GetActiveScene() { return default(Scene); }
        public static void LoadScene(string name) { }
    }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public delegate void UnityAction<T0>(T0 arg0);
    public delegate void UnityAction<T0, T1>(T0 arg0, T1 arg1);
    public class UnityEvent { public void AddListener(UnityAction call) { } }
}

namespace UnityEngine.UI
{
    public class Button : MonoBehaviour { public Events.UnityEvent onClick = new Events.UnityEvent(); public bool interactable; }
    public class Image : MonoBehaviour { }
}

namespace TMPro
{
    public class TextMeshProUGUI : UnityEngine.MonoBehaviour { public string text; }
    public class TMP_InputField : UnityEngine.MonoBehaviour { public string text; }
}
