// Texture packer by Samuel Roy
// Uses code from https://github.com/mfascia/TexturePacker
// Uses code from ExportAllTextures.csx
// Uses code from ReduceEmbeddedTexturePages.csx
// Script by Raltyro

using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

// "(.+?)" - match everything; "?" = match as few characters as possible.
// "(?:_(-*\d+))*" - an underscore + (optional minus + several digits);
// "?:" = don't make a separate group for the whole part, "*" = make this part optional.
Regex sprFrameRegex = new(@"^(.+?)(?:_(-*\d+))*$", RegexOptions.Compiled);

// Get directory path
DirectoryInfo dir = Directory.CreateDirectory(Path.Combine(ExePath, "Packager"));

// Clear any files if they already exist
foreach (FileInfo file in dir.GetFiles()) file.Delete();
foreach (DirectoryInfo di in dir.GetDirectories()) di.Delete(true);

string importFolder = PromptChooseDirectory();
string outName = dir.FullName + Path.DirectorySeparatorChar + "atlas";

SetProgressBar(null, "Packing Textures to Atlases", 0, 1);
StartProgressBarUpdater();

// Run the texture packer using borrowed and slightly modified code from the
// Texture packer sourced above
Packer packer = new Packer();
packer.Process(importFolder, "*.png", 2048, 1);
//packer.SaveAtlasses(outName);
packer.Close();

await StopProgressBarUpdater();

SetProgressBar(null, "Importing Atlases", 0, 1);
StartProgressBarUpdater();

int lastTextPage = Data.EmbeddedTextures.Count - 1;
int lastTextPageItem = Data.TexturePageItems.Count - 1;

string prefix = outName;
int atlasCount = 0;

foreach (Atlas atlas in packer.Atlasses) {
	string atlasName = String.Format(prefix + "{0:0000}" + ".png", atlasCount);
	Bitmap atlasBitmap = new Bitmap(atlasName);

	UndertaleEmbeddedTexture texture = new UndertaleEmbeddedTexture();
	texture.Name = new UndertaleString("Texture " + ++lastTextPage);
	texture.TextureData.TextureBlob = packer.Worker.GetImageBytes(packer.CreateAtlasImage(atlas));
	Data.EmbeddedTextures.Add(texture);

	foreach (Node n in atlas.Nodes) {
		if (n.Texture == null) continue;

		// Initalize values of this texture
		UndertaleTexturePageItem texturePageItem = new UndertaleTexturePageItem();
		texturePageItem.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
		texturePageItem.SourceX = (ushort)n.Bounds.X;
		texturePageItem.SourceY = (ushort)n.Bounds.Y;
		texturePageItem.SourceWidth = (ushort)n.Bounds.Width;
		texturePageItem.SourceHeight = (ushort)n.Bounds.Height;
		texturePageItem.TexturePage = texture;

		// Add this texture to UMT
		Data.TexturePageItems.Add(texturePageItem);

		// String processing
		string stripped = Path.GetFileNameWithoutExtension(n.Texture.Source);
		SpriteType spriteType = GetSpriteType(n.Texture.Source);

		if ((spriteType == SpriteType.Unknown) || (spriteType == SpriteType.Font)) spriteType = SpriteType.Sprite;

		setTextureTargetBounds(texturePageItem, stripped, n);

		if (spriteType == SpriteType.Background) {
			UndertaleBackground background = Data.Backgrounds.ByName(stripped);
			if (background != null)
				background.Texture = texturePageItem;
			else {
				// No background found, let's make one
				UndertaleString backgroundUTString = Data.Strings.MakeString(stripped);
				UndertaleBackground newBackground = new UndertaleBackground();
				newBackground.Name = backgroundUTString;
				newBackground.Transparent = false;
				newBackground.Preload = false;
				newBackground.Texture = texturePageItem;
				Data.Backgrounds.Add(newBackground);
			}
		}
		else if (spriteType == SpriteType.Sprite) {
			// Get sprite to add this texture to
			string spriteName;
			int frame = 0;

			try {
				var spriteParts = sprFrameRegex.Match(stripped);
				spriteName = spriteParts.Groups[1].Value;
				Int32.TryParse(spriteParts.Groups[2].Value, out frame);
			}
			catch (Exception e) {
				ScriptMessage("Error: Image " + stripped + " has an invalid name. Skipping...");
				continue;
			}
			UndertaleSprite sprite = null;
			sprite = Data.Sprites.ByName(spriteName);

			// Create TextureEntry object
			UndertaleSprite.TextureEntry texentry = new UndertaleSprite.TextureEntry();
			texentry.Texture = texturePageItem;

			// Set values for new sprites
			if (sprite == null) {
				UndertaleString spriteUTString = Data.Strings.MakeString(spriteName);
				UndertaleSprite newSprite = new UndertaleSprite();
				newSprite.Name = spriteUTString;
				newSprite.Width = (uint)n.Bounds.Width;
				newSprite.Height = (uint)n.Bounds.Height;
				newSprite.MarginLeft = 0;
				newSprite.MarginRight = n.Bounds.Width - 1;
				newSprite.MarginTop = 0;
				newSprite.MarginBottom = n.Bounds.Height - 1;
				newSprite.OriginX = 0;
				newSprite.OriginY = 0;

				if (frame > 0) {
					for (int i = 0; i < frame; i++) newSprite.Textures.Add(null);
				}

				newSprite.CollisionMasks.Add(newSprite.NewMaskEntry());

				Rectangle bmpRect = new Rectangle(n.Bounds.X, n.Bounds.Y, n.Bounds.Width, n.Bounds.Height);

				System.Drawing.Imaging.PixelFormat format = atlasBitmap.PixelFormat;
				Bitmap cloneBitmap = atlasBitmap.Clone(bmpRect, format);

				int width = ((n.Bounds.Width + 7) / 8) * 8;

				BitArray maskingBitArray = new BitArray(width * n.Bounds.Height);
				for (int y = 0; y < n.Bounds.Height; y++) {
					for (int x = 0; x < n.Bounds.Width; x++) {
						Color pixelColor = cloneBitmap.GetPixel(x, y);
						maskingBitArray[y * width + x] = (pixelColor.A > 0);
					}
				}

				BitArray tempBitArray = new BitArray(width * n.Bounds.Height);
				for (int i = 0; i < maskingBitArray.Length; i += 8) {
					for (int j = 0; j < 8; j++)
						tempBitArray[j + i] = maskingBitArray[-(j - 7) + i];
				}

				int numBytes = maskingBitArray.Length / 8;
				byte[] bytes = new byte[numBytes];

				tempBitArray.CopyTo(bytes, 0);
				for (int i = 0; i < bytes.Length; i++)
					newSprite.CollisionMasks[0].Data[i] = bytes[i];

				newSprite.Textures.Add(texentry);
				Data.Sprites.Add(newSprite);

				continue;
			}
			if (frame > sprite.Textures.Count - 1) {
				while (frame > sprite.Textures.Count - 1) sprite.Textures.Add(texentry);
				continue;
			}
			sprite.Textures[frame] = texentry;
		}
	}
	// Increment atlas
	atlasCount++;
}

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Importing Textures Complete.");

void setTextureTargetBounds(UndertaleTexturePageItem tex, string textureName, Node n) {
	tex.TargetX = 0;
	tex.TargetY = 0;
	tex.TargetWidth = (ushort)n.Texture.TargetWidth;
	tex.TargetHeight = (ushort)n.Texture.TargetHeight;
	tex.BoundingWidth = (ushort)n.Texture.TargetWidth;
	tex.BoundingHeight = (ushort)n.Texture.TargetHeight;
}

SpriteType GetSpriteType(string path) {
	string folderPath = Path.GetDirectoryName(path);
	string folderName = new DirectoryInfo(folderPath).Name;
	string lowerName = folderName.ToLower();

	if (lowerName == "backgrounds" || lowerName == "background") return SpriteType.Background;
	else if (lowerName == "fonts" || lowerName == "font") return SpriteType.Font;
	else if (lowerName == "sprites" || lowerName == "sprite") return SpriteType.Sprite;
	return SpriteType.Unknown;
}

string GetFolder(string path) {return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;}

//////////////////////////////////////////////////////////////////////////////////////////////////////
public class TextureInfo {
	public string Source;
	public Bitmap Image;
	public int Width;
	public int Height;
	public int TargetWidth;
	public int TargetHeight;
}

public enum SpriteType {
	Sprite,
	Background,
	Font,
	Unknown
}

public enum SplitType {
	Horizontal,
	Vertical,
}

public enum BestFitHeuristic {
	Area,
	MaxOneAxis,
}

public class Node {
	public Rectangle Bounds;
	public TextureInfo Texture;
	public SplitType SplitType;
}

public class Atlas {
	public int Width;
	public int Height;
	public List<Node> Nodes;
}

public class Packer {
	public TextureWorker Worker;
	public List<TextureInfo> SourceTextures;
	public int Padding;
	public int MaxAtlasSize;
	public List<Atlas> Atlasses;
	public StringWriter Log;
	public StringWriter Error;

	public Packer() {
		Worker = new TextureWorker();
		SourceTextures = new List<TextureInfo>();
		Log = new StringWriter();
		Error = new StringWriter();
	}

	public void Process(string sourceDir, string pattern, int maxAtlasSize, int padding) {
		Padding = padding;
		MaxAtlasSize = maxAtlasSize;

		ScanForTextures(sourceDir, pattern);
		List<TextureInfo> textures = SourceTextures.ToList();

		Atlasses = new List<Atlas>();
		while (textures.Count > 0) {
			Atlas atlas = new Atlas();
			//atlas.Width = 128;
			//atlas.Height = 128;
			atlas.Width = MaxAtlasSize;
			atlas.Height = MaxAtlasSize;

			List<TextureInfo> leftovers = LayoutAtlas(textures, atlas);
			/*
			while (leftovers.Count > 0) {
				if (atlas.Nodes[atlas.Nodes.Count - 1].SplitType == SplitType.Horizontal && atlas.Width < MaxAtlasSize) atlas.Width *= 2;
				else if (atlas.Height < MaxAtlasSize) atlas.Height *= 2;
				else break;

				leftovers = LayoutAtlas(textures, atlas);
			}
			*/
			while (leftovers.Count == 0) {
				atlas.Width /= 2;
				atlas.Height /= 2;
				leftovers = LayoutAtlas(textures, atlas);
			}
			if (leftovers.Count > 0) {
				atlas.Width *= 2;
				atlas.Height *= 2;
				leftovers = LayoutAtlas(textures, atlas);
			}

			Atlasses.Add(atlas);
			textures = leftovers;
		}
	}

	public void Close() {
		foreach (TextureInfo ti in SourceTextures) {
			ti.Image.Dispose();
			ti.Image = null;
		}
		Worker.Cleanup();
	}

	private void TrimTexture(TextureInfo ti) {
		Bitmap img = ti.Image;

		ti.TargetWidth = img.Width;
		ti.TargetHeight = img.Height;

		// https://gist.github.com/ttalexander2/88a40eec0fd0ea5b31cc2453d6bbddad
		Rectangle srcRect = new Rectangle(0, 0, ti.TargetWidth, ti.TargetHeight);
		BitmapData data = null;

		try {
			data = img.LockBits(new Rectangle(0, 0, ti.TargetWidth, ti.TargetHeight), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			byte[] buffer = new byte[data.Height * data.Stride];
			Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
			int xMin = img.Width; int xMax = 0;
			int yMin = img.Height; int yMax = 0;
			for (int y = 0; y < data.Height; y++) {
				for (int x = 0; x < data.Width; x++) {
					byte alpha = buffer[y * data.Stride + 4 * x + 3];
					if (alpha != 0) {
						if (x < xMin) xMin = x; if (x > xMax) xMax = x;
						if (y < yMin) yMin = y; if (y > yMax) yMax = y;
					}
				}
			}
			
			if (xMax < xMin || yMax < yMin) {
				ti.Width = 0;
				ti.Height = 0;
				return;
			}

			srcRect = Rectangle.FromLTRB(xMin, yMin, xMax + 1, yMax + 1);
		}
		finally {
			if (data != null) img.UnlockBits(data);
			else {
				ti.Width = ti.TargetWidth;
				ti.Height = ti.TargetHeight;
			}
		}

		Bitmap dest = new Bitmap(srcRect.Width, srcRect.Height);
		Graphics g = Graphics.FromImage(dest);
		g.DrawImage(img, new Rectangle(0, 0, srcRect.Width, srcRect.Height), srcRect, GraphicsUnit.Pixel);
		g.Dispose();

		ti.Image = dest;
		ti.Width = dest.Width;
		ti.Height = dest.Height;
	}

	private void ScanForTextures(string path, string wildcard) {
		DirectoryInfo fi = new DirectoryInfo(path);
		FileInfo[] files = fi.GetFiles(wildcard, SearchOption.AllDirectories);

		foreach (FileInfo file in files) {
			Bitmap img = TextureWorker.ReadImageFromFile(file.FullName);

			if (img != null) {
				TextureInfo ti = new TextureInfo();

				ti.Source = file.FullName;
				ti.Image = img;

				TrimTexture(ti);
				if (img.Width > MaxAtlasSize || img.Height > MaxAtlasSize || img.Width < 1 || img.Height < 1) {
					ti.Image.Dispose();
					break;
				}

				SourceTextures.Add(ti);
			}
		}
	}

	private TextureInfo FindBestFitForNode(Node _Node, List<TextureInfo> _Textures) {
		TextureInfo bestFit = null;
		float nodeArea = _Node.Bounds.Width * _Node.Bounds.Height;
		float maxCriteria = 0.0f;

		foreach (TextureInfo ti in _Textures) {
			if (ti.Width <= _Node.Bounds.Width && ti.Height <= _Node.Bounds.Height) {
				float wRatio = (float)ti.Width / (float)_Node.Bounds.Width;
				float hRatio = (float)ti.Height / (float)_Node.Bounds.Height;
				float ratio = wRatio > hRatio ? wRatio : hRatio;
				if (ratio > maxCriteria) {
					maxCriteria = ratio;
					bestFit = ti;
				}
				/*
				float textureArea = ti.Width * ti.Height;
				float coverage = textureArea / nodeArea;
				if (coverage > maxCriteria) {
					maxCriteria = coverage;
					bestFit = ti;
				}
				*/
			}
			break;
		}

		return bestFit;
	}

	private void HorizontalSplit(Node _ToSplit, int _Width, int _Height, List<Node> _List) {
		Node n1 = new Node();
		n1.Bounds.X = _ToSplit.Bounds.X + _Width + Padding;
		n1.Bounds.Y = _ToSplit.Bounds.Y;
		n1.Bounds.Width = _ToSplit.Bounds.Width - _Width - Padding;
		n1.Bounds.Height = _Height;
		n1.SplitType = SplitType.Vertical;

		Node n2 = new Node();
		n2.Bounds.X = _ToSplit.Bounds.X;
		n2.Bounds.Y = _ToSplit.Bounds.Y + _Height + Padding;
		n2.Bounds.Width = _ToSplit.Bounds.Width;
		n2.Bounds.Height = _ToSplit.Bounds.Height - _Height - Padding;
		n2.SplitType = SplitType.Horizontal;

		if (n1.Bounds.Width > 0 && n1.Bounds.Height > 0) _List.Add(n1);
		if (n2.Bounds.Width > 0 && n2.Bounds.Height > 0) _List.Add(n2);
	}

	private void VerticalSplit(Node _ToSplit, int _Width, int _Height, List<Node> _List) {
		Node n1 = new Node();
		n1.Bounds.X = _ToSplit.Bounds.X + _Width + Padding;
		n1.Bounds.Y = _ToSplit.Bounds.Y;
		n1.Bounds.Width = _ToSplit.Bounds.Width - _Width - Padding;
		n1.Bounds.Height = _ToSplit.Bounds.Height;
		n1.SplitType = SplitType.Vertical;

		Node n2 = new Node();
		n2.Bounds.X = _ToSplit.Bounds.X;
		n2.Bounds.Y = _ToSplit.Bounds.Y + _Height + Padding;
		n2.Bounds.Width = _Width;
		n2.Bounds.Height = _ToSplit.Bounds.Height - _Height - Padding;
		n2.SplitType = SplitType.Horizontal;

		if (n1.Bounds.Width > 0 && n1.Bounds.Height > 0) _List.Add(n1);
		if (n2.Bounds.Width > 0 && n2.Bounds.Height > 0) _List.Add(n2);
	}

	private List<TextureInfo> LayoutAtlas(List<TextureInfo> textures, Atlas atlas) {
		atlas.Nodes = new List<Node>();

		List<TextureInfo> leftovers = textures.ToList();
		List<Node> freeList = new List<Node>();

		Node root = new Node();
		root.Bounds.Size = new Size(atlas.Width, atlas.Height);
		root.SplitType = SplitType.Horizontal;
		freeList.Add(root);

		while (freeList.Count > 0 && leftovers.Count > 0) {
			Node node = freeList[0];
			freeList.RemoveAt(0);

			TextureInfo bestFit = FindBestFitForNode(node, leftovers);

			if (bestFit != null) {
				if (node.SplitType == SplitType.Horizontal)
					HorizontalSplit(node, bestFit.Width, bestFit.Height, freeList);
				else
					VerticalSplit(node, bestFit.Width, bestFit.Height, freeList);

				node.Texture = bestFit;
				node.Bounds.Width = bestFit.Width;
				node.Bounds.Height = bestFit.Height;
				leftovers.Remove(bestFit);
			}

			atlas.Nodes.Add(node);
		}

		return leftovers;
	}

	public void SaveAtlasses(string destination) {
		int atlasCount = 0;
		string prefix = destination;
		string descFile = destination + ".txt";

		StreamWriter tw = new StreamWriter(descFile);
		tw.WriteLine("atlas_tex, x, y, width, height, targetWidth, targetHeight");

		foreach (Atlas atlas in Atlasses) {
			string atlasName = String.Format(prefix + "{0:0000}" + ".png", atlasCount);

			Image img = CreateAtlasImage(atlas);
			img.Save(atlasName, System.Drawing.Imaging.ImageFormat.Png);

			foreach (Node n in atlas.Nodes) {
				if (n.Texture == null) continue;

				tw.Write(atlasName + ", ");
				tw.Write((n.Bounds.X).ToString() + ", " + (n.Bounds.Y).ToString() + ", ");
				tw.Write((n.Bounds.Width).ToString() + ", " + (n.Bounds.Height).ToString() + ", ");
				tw.WriteLine((n.Texture.TargetWidth).ToString() + ", " + (n.Texture.TargetHeight).ToString());
			}

			++atlasCount;
		}

		tw.Close();

		tw = new StreamWriter(prefix + "_log.txt");
		tw.WriteLine("--- LOG -------------------------------------------");
		tw.WriteLine(Log.ToString());
		tw.WriteLine("--- ERROR -----------------------------------------");
		tw.WriteLine(Error.ToString());
		tw.Close();
	}

	private Image CreateAtlasImage(Atlas atlas) {
		Image img = new Bitmap(atlas.Width, atlas.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
		Graphics graph = Graphics.FromImage(img);

		foreach (Node n in atlas.Nodes)
			if (n.Texture != null) graph.DrawImage(n.Texture.Image, n.Bounds);

		// DPI FIX START
		Bitmap ResolutionFix = new Bitmap(img);
		ResolutionFix.SetResolution(96.0f, 96.0f);
		Image img2 = ResolutionFix;
		// DPI FIX END

		return img2;
	}
}
