using System;

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
    
    public class RemoteBundle
    {
        public string BundleUrl  { get; set; }
        public string BundleHash { get; set; }
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

        public RemoteBundle StandaloneMac { get; set; }

        public RemoteBundle Standalone  { get; set; }
        public RemoteBundle Oculus      { get; set; }
        public RemoteBundle Android     { get; set; }
        public RemoteBundle iOS         { get; set; }
        public RemoteBundle Hololens    { get; set; }

        /// <summary>
        /// Returns the bundle for the given platform.
        /// </summary>
        /// <param name="platform"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public RemoteBundle this[Platform platform]
        {
            get
            {
                switch (platform)
                {
                    case Platform.EDITOR:
                    case Platform.STANDALONE: return Standalone;
                    
                    case Platform.EDITOR_MAC:
                    case Platform.STANDALONE_MAC: return StandaloneMac;
                    
                    case Platform.OCULUS:         return Oculus;
                    case Platform.ANDROID:        return Android;
                    case Platform.IOS:            return iOS;
                    case Platform.HOLOLENS:       return Hololens;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
                }
            }

            set
            {
                switch (platform)
                {
                    case Platform.EDITOR:
                    case Platform.STANDALONE: 
                        Standalone = value;
                        break;
                    
                    case Platform.EDITOR_MAC:
                    case Platform.STANDALONE_MAC:
                        StandaloneMac = value;
                        break;
                    
                    case Platform.OCULUS:     
                        Oculus = value;
                        break;
                    
                    case Platform.ANDROID:    
                        Android = value;
                        break;
                    
                    case Platform.IOS:
                        iOS = value;
                        break;
                    
                    case Platform.HOLOLENS:   
                        Hololens = value;
                        return;
                    
                    default:
                        throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
                }
            }
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