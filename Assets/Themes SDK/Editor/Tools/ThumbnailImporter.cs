using System;
using OpenUp.Editor.EnvironmentsSdk;
using UnityEditor;
using UnityEngine;

public class PostprocessImages : AssetPostprocessor
{
    private void OnPostprocessTexture(Texture2D texture)
    {
        // Filter out anything that isn't one of the generated thumbnails textures
        if (!texture.name.StartsWith(ThumbnailGenerator.ThumbnailPrepend))
            return;
        
        if (!Guid.TryParse(texture.name.Substring(ThumbnailGenerator.ThumbnailPrepend.Length), out Guid guid))
            return;
        
        TextureImporter textureImporter = (TextureImporter)assetImporter;
        textureImporter.textureType = TextureImporterType.Sprite;
    }
}
