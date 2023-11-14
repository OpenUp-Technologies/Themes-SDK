namespace OpenUp.Editor
{
    /// <summary>
    /// Any class implementing this will be created and shown in the main OpenUp menu.  
    /// </summary>
    /// <remarks>
    /// The main menu instantiates this object without arguments,
    /// so any inheritor must have a parameterless constructor.
    /// </remarks>
    public interface IMainFeatureSettings
    {
        /// <summary>
        /// This is used as header for the partial menu 
        /// </summary>
        string Header { get; }
        
        /// <summary>
        /// Priority determines the order of the partial menu items.
        /// Highest priority at the top, lowest at the bottom.
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Is called when a partial menu item is created.
        /// </summary>
        void OnActivate();
        
        /// <summary>
        /// Is called every GUI tick of the editor. Put calls to <see cref="UnityEditor.EditorGUILayout"/>
        /// to render your menu item here.
        /// </summary>
        void OnGUI();
    }
}