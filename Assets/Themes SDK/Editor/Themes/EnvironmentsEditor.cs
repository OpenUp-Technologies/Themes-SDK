using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenUp.Environment;
using OpenUp.Interpreter.Environment;
using OpenUp.Networking;
using OpenUp.Networking.Editor;
using UnityEditor;
using UnityEngine;

namespace OpenUp.Editor.EnvironmentsSdk
{
    public class EnvironmentsEditor
    {
        private const float REFRESH_TIMEOUT = 3;

        public static EnvironmentsEditor Instance => _Instance.Value;
        private static Lazy<EnvironmentsEditor> _Instance = new Lazy<EnvironmentsEditor>();
        
        public EnvironmentOption[] localOptions { get; private set; } = new EnvironmentOption[0];
        public List<RemoteOption> remoteOptions { get; private set; } = new List<RemoteOption>();
        private Task fetchTask = null;
        public bool isFetchingRemotes => fetchTask is { IsCompleted: false };
        private DeveloperProfile profile => DeveloperProfile.Instance;
        public Permissions permissions { get; private set; }
        private DateTime lastFetch = DateTime.Now;

        public void FindLocalOptions()
        {
            string[] guids = AssetDatabase.FindAssets("t:EnvironmentOption");

            localOptions = guids.Select(AssetDatabase.GUIDToAssetPath)
                                .Select(AssetDatabase.LoadAssetAtPath<EnvironmentOption>)
                                .ToArray();
        }
        
        /// <summary>
        /// Populates the <see cref="remoteOptions"/> field via a call to the API server. Does nothing if
        /// the fetch is currently running.
        /// </summary>
        public void FetchRemotes()
        {
            if (isFetchingRemotes) return;

            if (profile != null)
                fetchTask = _FetchRemotes();

            FindLocalOptions();
        }

        /// <summary>
        /// Runs the <see cref="FetchRemotes"/> method if that hasn't been done in a while. 
        /// </summary>
        public void GuiTick()
        {
            bool isOutdated = lastFetch + TimeSpan.FromSeconds(REFRESH_TIMEOUT) < DateTime.Now;
            bool isLoggedIn = profile?.isLoggedIn ?? false;
            
            if (isOutdated && isLoggedIn) 
                FetchRemotes();
        }

        private async Task _FetchRemotes()
        {
            try
            {
                SimpleApiCaller caller = await profile.GetSignedCaller();

                remoteOptions = await caller.GetAsync<List<RemoteOption>>("environments");
                remoteOptions.Sort(SortOptions);

                permissions = await caller.GetAsync<Permissions>("permissions");
                lastFetch = DateTime.Now;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// Sorts the remote options so that the options that are present in the project are first, then your own public options,
        /// then your private options, then other public options.
        /// </summary>
        /// <param name="optionA"></param>
        /// <param name="optionB"></param>
        /// <returns></returns>
        private int SortOptions(RemoteOption optionA, RemoteOption optionB)
        {
            bool hasLocalA = localOptions.Any(opt => opt.databaseId == optionA.Id);
            bool hasLocalB = localOptions.Any(opt => opt.databaseId == optionB.Id);

            if (hasLocalA  && !hasLocalB) return -1;
            if (!hasLocalA && hasLocalB) return 1;

            bool isAuthorA = optionA.Author?.Id == DeveloperProfile.Instance.userId;
            bool isAuthorB = optionB.Author?.Id == DeveloperProfile.Instance.userId;

            if (isAuthorA  && !isAuthorB) return -1;
            if (!isAuthorA && isAuthorB) return 1;

            if (!optionA.IsPrivate && optionB.IsPrivate) return -1;
            if (optionA.IsPrivate  && !optionB.IsPrivate) return 1;

            return 0;
        }

        public record Permissions(bool isDeveloper, bool canManage, Dictionary<string, Organisation> organisations, List<string> roles);
        public record Organisation(string orgId, string orgName, bool isAdmin, bool isOwner, bool canManage,
                                   List<string> managerOf, List<string> memberOf, List<string> visibleGroups);
    }
}