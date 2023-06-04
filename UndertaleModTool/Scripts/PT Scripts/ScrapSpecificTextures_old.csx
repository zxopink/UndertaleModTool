using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

string texFolder = GetFolder(FilePath) + "Export_Sprites" + Path.DirectorySeparatorChar;
Directory.CreateDirectory(texFolder);

SetProgressBar(null, "Sprites", 0, Data.Sprites.Count);
StartProgressBarUpdater();

List<string> spritesToDelete = GetUserInputSprites();

await DeleteTextures();

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Texture deletion complete.");

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

async Task DeleteTextures()
{
    await Task.Run(() => Parallel.ForEach(Data.Sprites, DeleteSpriteTextures));
}

void DeleteSpriteTextures(UndertaleSprite sprite)
{
    string spriteName = sprite.Name.Content.ToLower();

    if (spritesToDelete.Contains(spriteName))
    {
        sprite.Textures.Clear(); // Remove all texture entries in sprite. Doesn't delete the texture in their texture page.
    }

    IncrementProgressParallel();
}

List<string> GetUserInputSprites()
{
    List<string> spriteNames = new List<string>();
    string inputtedText = "";

    inputtedText = SimpleTextInput("Menu", "Enter sprite names (one per line)", inputtedText, true);
    string[] individualLineArray = inputtedText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    foreach (var line in individualLineArray)
    {
        spriteNames.Add(line.Trim().ToLower());
    }

    return spriteNames;
}