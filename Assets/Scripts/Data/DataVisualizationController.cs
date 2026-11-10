// using UnityEngine;
// using System.Collections;
// using System.IO;
// using System;
// using System.Collections.Generic;
// using System.Runtime.InteropServices;
// using UnityEngine.Networking;
// using System.Text.RegularExpressions;
// using UnityEngine.EventSystems;
// using UnityEngine.Rendering;
// using UnityEngine.UI;
// using UnityEngine.Android;


// public class DataVisualizationController : MonoBehaviour
// {
//     [Header("WebView References")]
//     [Tooltip("Drag the WebViewPrefab from your scene here.")]
//     public WebViewPrefab webViewPrefab;

//     [Tooltip("Drag your chart_template.html (TextAsset) here from the Project window.")]
//     public TextAsset chartHtmlTemplate; // Assign your chart_template.html here

//     [Header("Canvas Reference")]
//     [Tooltip("The parent GameObject for the WebView, often a UI Panel.")]
//     public RectTransform webViewContainer; // This will be the parent for the WebViewPrefab's UI

//     private WebView _webView;
//     private UserProgressManager _userProgressManager;

//     void Awake()
//     {
//         if (webViewPrefab == null)
//         {
//             Debug.LogError("WebViewPrefab is not assigned in DataVisualizationController.");
//             enabled = false;
//             return;
//         }

//         if (chartHtmlTemplate == null)
//         {
//             Debug.LogError("chartHtmlTemplate (TextAsset) is not assigned in DataVisualizationController.");
//             enabled = false;
//             return;
//         }

//         // Initialize WebView and parent it to the container
//         webViewPrefab.Init();
//         _webView = webViewPrefab.WebView;

//         // Set the parent of the WebViewPrefab to the designated container
//         webViewPrefab.gameObject.transform.SetParent(webViewContainer, false);
//         // Ensure it fills the container
//         webViewPrefab.RectTransform.anchorMin = Vector2.zero;
//         webViewPrefab.RectTransform.anchorMax = Vector2.one;
//         webViewPrefab.RectTransform.sizeDelta = Vector2.zero;
//         webViewPrefab.RectTransform.anchoredPosition = Vector2.zero;
//     }

//     void Start()
//     {
//         _userProgressManager = UserProgressManager.Instance;
//         if (_userProgressManager == null)
//         {
//             Debug.LogError("UserProgressManager.Instance is null. Ensure UserProgressManager is in the scene.");
//             enabled = false;
//             return;
//         }

//         // Load the HTML content once the WebView is ready
//         // _webView.OnPageFinished += (url) => {
//         //     Debug.Log("WebView finished loading: " + url);
//         //     // You might want to call GenerateAndDisplayCharts() here if you load a web URL
//         // };

//         // Load HTML directly from the TextAsset
//         LoadHtmlIntoWebView();
//     }

//     private void LoadHtmlIntoWebView()
//     {
//         if (_webView != null && chartHtmlTemplate != null)
//         {
//             // This is the content of your HTML file
//             string htmlContent = chartHtmlTemplate.text;
//             // Load HTML content directly
//             _webView.LoadHtml(htmlContent);

//             // Add a listener for when the page is loaded to then inject data
//             _webView.OnLoaded += (url) => {
//                 Debug.Log("HTML loaded into WebView. Preparing to send data.");
//                 GenerateAndDisplayCharts();
//             };
//         }
//     }

//     // Call this method whenever you want to refresh the graphs
//     public void GenerateAndDisplayCharts()
//     {
//         if (_userProgressManager == null || _webView == null)
//         {
//             Debug.LogWarning("Cannot generate charts: UserProgressManager or WebView not ready.");
//             return;
//         }

//         // 1. Get the latest user progress data
//         UserProgressData progressData = _userProgressManager.currentUserProgress;

//         // 2. Convert the C# object to a JSON string
//         // We need a wrapper if UserProgressData itself isn't directly serializable without a root element
//         // or if we want to send only a part of it.
//         // JsonUtility.ToJson works well with [Serializable] classes.
//         string jsonData = JsonUtility.ToJson(progressData);
//         Debug.Log("Sending data to WebView: " + jsonData.Substring(0, Mathf.Min(jsonData.Length, 200)) + "..."); // Log first 200 chars

//         // 3. Execute JavaScript function in the WebView to update charts
//         // The JavaScript function is 'updateCharts' and it expects one string argument (the JSON data)
//         _webView.ExecuteScript($"updateCharts({JsonUtility.ToJson(jsonData)})"); // Double-encode to send string
//         // Note: JsonUtility.ToJson(jsonData) converts the string `jsonData` into a JSON string,
        // which results in `\"` for quotes, perfect for JS parsing.
//     }

//     // Example: Call this from a UI Button's OnClick event
//     public void ShowAndRefreshCharts()
//     {
//         if (webViewContainer != null)
//         {
//             webViewContainer.gameObject.SetActive(true);
//             GenerateAndDisplayCharts();
//         }
//     }

//     public void HideCharts()
//     {
//         if (webViewContainer != null)
//         {
//             webViewContainer.gameObject.SetActive(false);
//         }
//     }
// }