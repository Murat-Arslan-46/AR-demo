using Firebase.Extensions;
using Firebase.Storage;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Vuforia;

public class SimpleCloudRecoEventHandler : MonoBehaviour
{
    private CloudRecoBehaviour mCloudRecoBehaviour; 
    public ImageTargetBehaviour ImageTargetTemplate;
    private bool mIsScanning = false;
    private string mTargetMetadata = "";
    public TMP_Text m_text;
    public GameObject m_video,m_image;
    private bool image = false, video = false;
    public bool s_download = false;
    string src,local,name;
    int n_image, num = 0;
    StorageReference reference; // gs://ar-demo-309912.appspot.com
    FirebaseStorage storage;
    void Awake()
    {
        mCloudRecoBehaviour = GetComponent<CloudRecoBehaviour>();
        mCloudRecoBehaviour.RegisterOnInitializedEventHandler(OnInitialized);
        mCloudRecoBehaviour.RegisterOnInitErrorEventHandler(OnInitError);
        mCloudRecoBehaviour.RegisterOnUpdateErrorEventHandler(OnUpdateError);
        mCloudRecoBehaviour.RegisterOnStateChangedEventHandler(OnStateChanged);
        mCloudRecoBehaviour.RegisterOnNewSearchResultEventHandler(OnNewSearchResult);
    }
    void OnDestroy()
    {
        mCloudRecoBehaviour.UnregisterOnInitializedEventHandler(OnInitialized);
        mCloudRecoBehaviour.UnregisterOnInitErrorEventHandler(OnInitError);
        mCloudRecoBehaviour.UnregisterOnUpdateErrorEventHandler(OnUpdateError);
        mCloudRecoBehaviour.UnregisterOnStateChangedEventHandler(OnStateChanged);
        mCloudRecoBehaviour.UnregisterOnNewSearchResultEventHandler(OnNewSearchResult);
    }
    public void OnInitialized(TargetFinder targetFinder)
    {
        Debug.Log("Cloud Reco initialized");
    }
    public void OnInitError(TargetFinder.InitState initError)
    {
        Debug.Log("Cloud Reco init error " + initError.ToString());
    }
    public void OnUpdateError(TargetFinder.UpdateState updateError)
    {
        Debug.Log("Cloud Reco update error " + updateError.ToString());
    }
    public void OnStateChanged(bool scanning)
    {
        mIsScanning = scanning;
        if (scanning)
        {
            // clear all known trackables
            var tracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
            tracker.GetTargetFinder<ImageTargetFinder>().ClearTrackables(false);
        }
    }
    public void OnNewSearchResult(TargetFinder.TargetSearchResult targetSearchResult)
    {
        TargetFinder.CloudRecoSearchResult cloudRecoSearchResult =
            (TargetFinder.CloudRecoSearchResult)targetSearchResult;
        // Build augmentation based on target 
        if (ImageTargetTemplate)
        {
            // enable the new result with the same ImageTargetBehaviour: 
            ObjectTracker tracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
            tracker.GetTargetFinder<ImageTargetFinder>().EnableTracking(targetSearchResult, ImageTargetTemplate.gameObject);
        }
        // do something with the target metadata
        mTargetMetadata = cloudRecoSearchResult.MetaData;
        // stop the target finder (i.e. stop scanning the cloud)
        mCloudRecoBehaviour.CloudRecoEnabled = false;
    }
    void OnGUI()
    {
        if (!mIsScanning)
        {
            if (GUI.Button(new Rect(350, 100, 200, 50), "QR kod ara"))
            {
                mCloudRecoBehaviour.CloudRecoEnabled = true;
            }
            string[] meta = mTargetMetadata.Split('#');
            name = meta[0];
            try { n_image = int.Parse(meta[1]); }
            catch(Exception e) { n_image = 0; }

            m_video.SetActive(video);
            m_image.SetActive(image);
            if (!image && !video)
            { InfoText(); }
            else if (image)
            { InfoImage(); }
            else if (video)
            { InfoVideo(); }
        }
    }
    void InfoText() {
        if (GUI.Button(new Rect(100, 100, 200, 50), "Video"))
        {
            image = false;
            video = true;
            src = "gs://ar-demo-309912.appspot.com/" + name + "/video.mp4";
            local = "video.mp4";
            DownloadFile();
        }
        if (GUI.Button(new Rect(100, 200, 200, 50), "Resim"))
        {
            video = false;
            image = true;
            src = "gs://ar-demo-309912.appspot.com/" + name + "/image.jpg";
            local = "image.jpg";
            DownloadFile();
        }

        if (s_download)
        {
            m_text.text = File.ReadAllText(Application.persistentDataPath + "/" + local) +"";
            s_download = false;
        }
    }
    void InfoImage()
    {
        if (GUI.Button(new Rect(100, 100, 200, 50), "Video"))
        {
            image = false;
            video = true; 
            src = "gs://ar-demo-309912.appspot.com/" + name + "/video.mp4";
            local = "video.mp4";
            DownloadFile();
        }
        if (GUI.Button(new Rect(100, 200, 200, 50), "Metin"))
        {
            video = false;
            image = false;
            src = "gs://ar-demo-309912.appspot.com/" + name + "/text.txt";
            local = "text.txt";
            DownloadFile();
        }
        if(n_image != 0)
        {
            if (GUI.Button(new Rect(100, 300, 200, 50), "Diğer resim"))
            {
                video = false;
                image = true;
                src = "gs://ar-demo-309912.appspot.com/" + name + "/image" + (num % n_image) + ".jpg";
                num++;
                local = "image.jpg";
                DownloadFile();
            }
        }

        if (s_download)
        {
            byte[] byteArray = File.ReadAllBytes(Application.persistentDataPath + "/" + local);
            Texture2D texture = new Texture2D(10, 10);
            texture.LoadImage(byteArray);
            Sprite s = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f,0.5f));
            SpriteRenderer renderer = m_image.GetComponent<SpriteRenderer>();
            renderer.sprite = s;
            s_download = false;
        }
    }
    void InfoVideo()
    {
        if (GUI.Button(new Rect(100, 100, 200, 50), "Metin"))
        {
            image = false;
            video = false;
            src = "gs://ar-demo-309912.appspot.com/" + name + "/text.txt";
            local = "text.txt";
            DownloadFile();
        }
        if (GUI.Button(new Rect(100, 200, 200, 50), "Resim"))
        {
            video = false;
            image = true;
            src = "gs://ar-demo-309912.appspot.com/" + name + "/image.jpg";
            local = "image.jpg";
            DownloadFile();
        }
        if (s_download)
        {
            VideoPlayer videoPlayer;
            AudioSource audioSource;
            videoPlayer = m_video.GetComponent<VideoPlayer>();
            audioSource = m_video.GetComponent<AudioSource>();
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, audioSource);
            videoPlayer.url = "file://" + Application.persistentDataPath + "/" + local;
            videoPlayer.Prepare();
            videoPlayer.Play();
            audioSource.Play();
            s_download = false;
        }
    }
    public void DownloadFile()
    {
        m_text.text = "Yükleniyor...";
        storage = FirebaseStorage.GetInstance("gs://ar-demo-309912.appspot.com");
        reference = storage.GetReferenceFromUrl(src);
        string Path = Application.persistentDataPath;

        if (File.Exists(Path + "/" + local))
            File.Delete(Path + "/" + local);
        
        reference.GetFileAsync(Path+"/"+local).ContinueWithOnMainThread(task => {
            if (!task.IsFaulted && !task.IsCanceled)
            {
                m_text.text = "";
                s_download = true;
            }
            if(task.IsFaulted)
            {
                m_text.text = "Dosyaya ulaşılamadı, böyle bir dosya olmayabilir";
            }
        });
    }
}