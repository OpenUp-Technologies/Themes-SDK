using System;
using System.Collections.Generic;
using OpenUp.Environment.Features;

namespace OpenUp.Interpreter.Environment
{
    /// <summary>
    /// Indicates which platform the bundle is intended for 
    /// </summary>
    public enum Platform
    {
        STANDALONE,
        STANDALONE_MAC,
        OCULUS,
        ANDROID,
        IOS,
        HOLOLENS,
        EDITOR,
        EDITOR_MAC
    }
    
    /// <summary>
    /// Contains the bundles for the Root Object, Scene, and Themed Objects.
    /// </summary>
    /// <remarks>
    /// Due to not being able to include the same object in different bundles, we have a line of dependency as follows:
    /// <li>The Scene Bundle depends on the Root Object Bundle.</li>
    /// <li>The Root Object Bundle depends on the Themed Objects Bundle.</li>
    /// </remarks>
    public class RemoteBundle
    {
        public string BundleUrl  { get; set; }
        public string BundleHash { get; set; }
    }

    /// <summary>
    /// How the server stores the themed objects.
    /// </summary>
    public record RemoteThemedObject(string DisplayName, string RelativePath, string Thumbnail)
    {
        public static RemoteThemedObject Create(ThemedObject themedObject) =>
            new RemoteThemedObject(themedObject.DisplayName, themedObject.RelativePath, null);
    }

    public class RemoteVersion : IEnvironmentOption
    {
        public string Name { get; set; }
        public string Id { get; set; }
        
        public string Version => BundleVersion;

        /// <summary>
        /// User defined version code for the environment. Use <see cref="Version"/> so parse.
        /// </summary>
        public string BundleVersion { get; set; }
        
        public string Thumbnail { get; set; }
        
        /// <summary>
        /// Version of the app used to build this environment, used in combination with <see cref="UsedScripts"/>
        /// to determine compatability issues. Use <see cref="Version"/> so parse.
        /// </summary>
        public string AppVersion { get; set; }
        
        public string[] UsedScripts { get; set; }
        public string[] PrefabPaths { get; set; }
        public RemoteThemedObject[] ThemedObjects { get; set; }
        
        public Dictionary<string, RemoteBundle> BundleCollection { get; set; }

        // These fields are being kept for backwards compatibility
        [Obsolete("Use BundleCollection instead")]
        public RemoteBundle StandaloneMac { get; set; }
        [Obsolete("Use BundleCollection instead")]
        public RemoteBundle Standalone  { get; set; }
        [Obsolete("Use BundleCollection instead")]
        public RemoteBundle Oculus      { get; set; }
        [Obsolete("Use BundleCollection instead")]
        public RemoteBundle Android     { get; set; }
        [Obsolete("Use BundleCollection instead")]
        public RemoteBundle iOS         { get; set; }
        [Obsolete("Use BundleCollection instead")]
        public RemoteBundle Hololens    { get; set; }

        /// <summary>
        /// Returns the bundle for the given platform.
        /// </summary>
        /// <param name="platform"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public RemoteBundle this[Platform platform]
        {
            // Try the dictionary, otherwise fallback to the dedicated fields.
            get => BundleCollection.GetValueOrDefault(platform.ToString()) ?? platform switch
                {
                    Platform.EDITOR or Platform.STANDALONE => Standalone,
                    Platform.EDITOR_MAC or Platform.STANDALONE_MAC => StandaloneMac,
                    Platform.OCULUS => Oculus,
                    Platform.ANDROID => Android,
                    Platform.IOS => iOS,
                    Platform.HOLOLENS => Hololens,
                    _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
                };
            set => BundleCollection[platform.ToString()] = value;
        }
    }
    
    public class RemoteOption
    {
        public string          Id           { get; set; }
        public string          Name         { get; set; }
        public UserInfo        Author       { get; set; }
        public string OrganisationId { get; set; }
        public string          ThumbnailUrl { get; set; }
        public bool            IsPrivate    { get; set; }
        public RemoteVersion[] Versions     { get; set; }
    }

    public class UserInfo {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}