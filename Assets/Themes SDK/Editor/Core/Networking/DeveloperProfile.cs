using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenUp.Utils;
using UnityEngine;
using UnityEditor;

namespace OpenUp.Networking.Editor
{
    /// <summary>
    /// Holds the data to connect to the OpenUp Platform as a developer.
    /// You probably want to make sure you don't push this to your VCS (a.k.a. git).
    /// </summary>
    [CreateAssetMenu(fileName = "MyProfile", menuName = "OpenUp/Editor/DeveloperProfile", order = 0)]
    public class DeveloperProfile : ScriptableObject
    {
        [SerializeField] public string username;
        [SerializeField] public string userId;
        [SerializeField] public string server = "https://prod-server.openuptech.com/";
        [SerializeField] private bool isActive;
        
        private string credentials;
        public bool isLoggedIn => credentials != null;
        public bool isLoggingIn { get; private set; }

        public static DeveloperProfile Instance => _Instance ??= GetInstance() ;
        private static DeveloperProfile _Instance;
        private static float lastInstanceCheck;
        
        private static DeveloperProfile GetInstance()
        {
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            if (now < lastInstanceCheck + 1000)
                return null;

            lastInstanceCheck = now;
            
            string[] guids = AssetDatabase.FindAssets("t:DeveloperProfile");

            DeveloperProfile[] profiles = guids.Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<DeveloperProfile>)
                .Where(profile => profile != null)
                .Where(profile => profile.isActive)
                .ToArray();

            if (profiles.Length == 0) return null;

            if (profiles.Length == 1) return profiles[0];

            for (int i = 1; i < profiles.Length; i++)
            {
                profiles[i].isActive = false;
                EditorUtility.SetDirty(profiles[i]);
            }
            
            AssetDatabase.SaveAssets();

            profiles[0].GetCredentials();

            return profiles[0];
        }

        public static void ClearActive()
        {
            string[] guids = AssetDatabase.FindAssets("t:DeveloperProfile");

            DeveloperProfile[] profiles = guids.Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<DeveloperProfile>)
                .Where(profile => profile)
                .Where(profile => profile.isActive)
                .ToArray();
            
            foreach (DeveloperProfile profile in profiles)
            {
                profile.isActive = false;
                EditorUtility.SetDirty(profile);
            }
            
            AssetDatabase.SaveAssets();
            _Instance = null;
        }

        public void SetActive()
        {
            ClearActive();

            isActive = true;
            AssetDatabase.SaveAssets();

            _Instance = this;
        }
        
        public async Task<SimpleApiCaller> GetSignedCaller(bool displayWindow = false)
        {
            SimpleApiCaller caller = new SimpleApiCaller(server);

            if (credentials != null)
            {
                caller.ConfigureAuthentication(credentials);
                return caller;
            }
            
            LoginResponseModel response = await AsyncHelperEditor.DoUnityWork(GetCredentials);

            if (response == null && displayWindow)
            {
                await AsyncHelperEditor.DoUnityWork(() =>
                {
                    LoginWindow.GetWindow(this);
                });
            }
            
            if (response == null)
            {
                throw new Exception("You are not logged in");
            }
            
            caller.ConfigureAuthentication(response.Credentials);
            
            await AsyncHelperEditor.DoUnityWork(() => StoreCredentials(response));

            return caller;
        }

        private void StoreCredentials(LoginResponseModel response)
        {
            SetId(response.Credentials.Split(':')[0]);
            credentials = response.Credentials;

            string loc = $"{Application.persistentDataPath}/editor_credentials_{name.GetHashCode()}";
            
            File.WriteAllText(loc, JsonConvert.SerializeObject(response));
        }

        private LoginResponseModel GetCredentials()
        {
            string loc = $"{Application.persistentDataPath}/editor_credentials_{name.GetHashCode()}";

            if (!File.Exists(loc)) return null;

            string contents = File.ReadAllText(loc);
            
            return JsonConvert.DeserializeObject<LoginResponseModel>(contents);
        }

        public void Authorize(string pass)
        {
            SimpleApiCaller caller = new SimpleApiCaller(server);
            isLoggingIn = true;

            Task.Run(async () =>
            {
                try
                {
                    LoginResponseModel response = await caller.PostAsync<LoginResponseModel>(
                        "login",
                        new { Username = username, Password = pass }
                    );

                    isLoggingIn = false;
                    
                    await AsyncHelperEditor.DoUnityWork(() => StoreCredentials(response));
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    isLoggingIn = false;
                }
            });
        }

        private void SetId(string id)
        {
            if (id == userId) return;

            userId = id;
            
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public void Logout()
        {
            credentials = null;
            isLoggingIn = false;
            File.Delete($"{Application.persistentDataPath}/editor_credentials_{name.GetHashCode()}");
        }

        private record LoginResponseModel(string Username, string Credentials);
    }
}