using UndertaleModLib;

string whereToSave = GetFolder(FilePath) + "Export_Sprites" + Path.DirectorySeparatorChar + "ok.txt";

StreamWriter tw = new StreamWriter(whereToSave);
List<string> hm = new List<string>();

foreach (UndertaleSprite sprite in Data.Sprites) {
	foreach (UndertaleSprite.TextureEntry entry in sprite.Textures) {
		if (entry == null) continue;

		if (entry.Texture == null) continue;
		UndertaleEmbeddedTexture embedTexture = entry.Texture.TexturePage;

		if (embedTexture == null) continue;
		string wah = embedTexture.Name.ToString().Trim();
		wah = wah.Substring(9, wah.Length - 10);

		if (int.Parse(wah) < 268) continue;
		string woh = sprite.Name.ToString().Trim();
		woh = woh.Substring(1, woh.Length - 2);

		if (!hm.Contains(woh)) {
			tw.WriteLine(woh);
			hm.Add(woh);
		}
	}
}

tw.Close();

string GetFolder(string path) {return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;}