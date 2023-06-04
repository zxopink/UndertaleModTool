using UndertaleModLib;

string whereToSave = GetFolder(FilePath) + "Export_Sprites" + Path.DirectorySeparatorChar + "ok.txt";

StreamWriter tw = new StreamWriter(whereToSave);
List<string> hm = new List<string>();

for (int i = Data.TexturePageItems.Count - 1; i >= 28805; i--) {
	Data.TexturePageItems[i]?.Dispose();
	Data.TexturePageItems.Remove(Data.TexturePageItems[i]);
}

for (int i = Data.EmbeddedTextures.Count - 1; i >= 268; i--) {
	Data.EmbeddedTextures[i]?.Dispose();
	Data.EmbeddedTextures.Remove(Data.EmbeddedTextures[i]);
}

tw.Close();

string GetFolder(string path) {return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;}