﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Unity.Editor;
using Firebase.Database;
using Firebase.Storage;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DatabaseManager : MonoBehaviour
{
    public GameObject downloadButton;
    public GameObject cameraButton;
    public GameObject textField;
    public GameObject incorrectTextField;
    private List<Entry> allEntries;
    private FirebaseStorage storage;
    private StorageReference storage_ref;
    private int downloadsRunning = 0;

    // Start is called before the first frame update
    void Start()
    {
        allEntries = new List<Entry>();
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://poppeg-95e96.firebaseio.com/");
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
        storage = FirebaseStorage.DefaultInstance;
        storage_ref = storage.GetReferenceFromUrl("gs://poppeg-95e96.appspot.com");
    }

    void Update()
    {
        if (downloadsRunning > 0)
        {
            downloadButton.GetComponentInChildren<Text>().text = "Downloading";
            cameraButton.SetActive(false);
        }

        if (downloadsRunning <= 0)
        {
            downloadButton.GetComponentInChildren<Text>().text = "Download";
            cameraButton.SetActive(true);
        }
    }

    public void GetData()
    {
        string bookName = GameObject.Find("BookName").GetComponent<Text>().text;
        Debug.Log(bookName);
        if (!string.IsNullOrEmpty(bookName))
        {
            FirebaseDatabase.DefaultInstance.RootReference.Child("targets").GetValueAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.Log("ERROR");
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (snapshot.HasChild(bookName))
                    {
                        DataSnapshot entries = snapshot.Child(bookName).Child("entries");
                        foreach (DataSnapshot entry in entries.Children)
                        {
                            string entryName = entry.Child("name").Value.ToString();
                            string entryURL = entry.Child("url").Value.ToString();
                            allEntries.Add(new Entry(entryName, entryURL));
                        }
                        DownloadVideo();
                    }
                    else {
                        incorrectTextField.SetActive(true);
                    }

                }

            });

        }
        else
        {
            GameObject.Find("Placeholder").GetComponent<Text>().color = Color.red;
        }

    }

    public void DownloadVideo()
    {
        foreach (Entry e in allEntries)
        {
            Debug.Log("NAME: " + e.GetName() + " URL: " + e.GetUrl());
            string entryUrl = e.GetUrl();
            string entryName = e.GetName();
            StorageReference gs_reference =
                storage.GetReferenceFromUrl("gs://poppeg-95e96.appspot.com/" + entryUrl);

            gs_reference.GetDownloadUrlAsync().ContinueWith((Task<Uri> task) =>
            {
                if (!task.IsFaulted && !task.IsCanceled)
                {
                    Debug.Log("Download URL: " + task.Result);
                    StartCoroutine(DownloadVideoFromURL(task.Result.ToString(), entryName));
                }
            });
        }
    }

    public void CameraScene()
    {
        SceneManager.LoadScene(1);
    }

    IEnumerator DownloadVideoFromURL(string url, string name)
    {
        downloadsRunning++;
        var www = new WWW(url);
        Debug.Log("Downloading!");
        yield return www;
        Debug.Log(Application.persistentDataPath);
        File.WriteAllBytes(Application.persistentDataPath + "/" + name + ".mp4", www.bytes);
        Debug.Log("File Saved!");
        downloadsRunning--;
    }
}



class Entry
{
    private string url;
    private string name;

    public Entry(string name, string url)
    {
        this.name = name;
        this.url = url;
    }

    public string GetUrl()
    {
        return url;
    }

    public string GetName()
    {
        return name;
    }
}