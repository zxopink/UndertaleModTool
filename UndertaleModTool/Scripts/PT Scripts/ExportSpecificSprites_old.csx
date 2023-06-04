// Modified code to allow exporting specific sprites by JQBUNI

using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

bool unpadded = ScriptQuestion("Export all sprites unpadded?");

string texFolder = GetFolder(FilePath) + "Export_Sprites" + Path.DirectorySeparatorChar;
TextureWorker worker = new TextureWorker();
Directory.CreateDirectory(texFolder);

SetProgressBar(null, "Sprites", 0, Data.Sprites.Count);
StartProgressBarUpdater();

List<string> spritesToExport = GetUserInputSprites();

await DumpSprites();
worker.Cleanup();

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Export Complete.");

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

async Task DumpSprites()
{
    await Task.Run(() => Parallel.ForEach(Data.Sprites, DumpSprite));
}

void DumpSprite(UndertaleSprite sprite)
{
    string spriteName = sprite.Name.Content.ToLower();
    
    if (spritesToExport.Contains(spriteName))
    {
        string spriteFolder = texFolder + sprite.Name.Content + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(spriteFolder);

        for (int i = 0; i < sprite.Textures.Count; i++)
        {
            if (sprite.Textures[i]?.Texture != null)
            {
                if (unpadded)
                {
                    worker.ExportAsPNG(sprite.Textures[i].Texture, spriteFolder + sprite.Name.Content + "_" + i + ".png", null, false); // Exclude padding on export
                }
                else
                {
                    worker.ExportAsPNG(sprite.Textures[i].Texture, spriteFolder + sprite.Name.Content + "_" + i + ".png", null, true); // Include padding to make sprites look neat
                }
            }
        }

        IncrementProgressParallel();
    }
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