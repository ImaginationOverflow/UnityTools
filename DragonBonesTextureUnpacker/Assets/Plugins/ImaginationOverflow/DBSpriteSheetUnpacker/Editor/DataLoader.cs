using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ImaginationOverflow.DBSpriteSheetUnpacker.Editor.Data;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ImaginationOverflow.DBSpriteSheetUnpacker.Editor
{
    public class DataLoader
    {
        public static DetailedTextureInfo Load(string path)
        {
            var raw = LoadRaw(path);

            var details = new DetailedTextureInfo();

            details.Animations = ExtractAnimations(raw.SubTexture);
            details.Original = raw;
            details.SpritePath = Path.Combine(Path.GetDirectoryName(path), raw.imagePath);
            details.FileName = Path.GetFileName(details.SpritePath).Replace(Path.GetExtension(details.SpritePath), string.Empty);
            return details;
        }

        private static List<DBAnimation> ExtractAnimations(SubTexture[] rawSubTexture)
        {
            var ret = new List<DBAnimation>();
            foreach (var t in rawSubTexture)
            {
                var animName = ExtractAnimName(t.name);

                var anim = ret.FirstOrDefault(r => r.Name == animName);

                if (anim == null)
                {
                    anim = new DBAnimation
                    {
                        Name = animName
                    };
                    ret.Add(anim);
                }

                anim.Sprites.Add(t);
            }

            return ret;
        }

        private static string ExtractAnimName(string name)
        {
            var separatorIdx = name.LastIndexOf('_');
            if (separatorIdx == -1)
                return null;

            return name.Substring(0, separatorIdx);
        }

        private static TextureInfo LoadRaw(string path)
        {
            return JsonUtility.FromJson<TextureInfo>(File.ReadAllText(path));

        }

        public static string ImportSprite(DetailedTextureInfo data, string dstPath, bool overrideSprite,
            bool[] selectedAnimations)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(dstPath)))
            {
                dstPath = Path.Combine(dstPath, Path.GetFileName(data.SpritePath));
            }
            File.Copy(data.SpritePath, dstPath, overrideSprite);


            var assetFilePath = GetProjectPathFromFullPath(dstPath);
            AssetDatabase.ImportAsset(assetFilePath);


            var importer = AssetImporter.GetAtPath(assetFilePath) as TextureImporter;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;

            var totalHeight = 0;
            int dummy = 0;

            GetImageSize(importer, out dummy, out totalHeight);


            var sprites = new List<SpriteMetaData>();
            int j = 0;
            foreach (var dataAnimation in data.Animations)
            {
                if(selectedAnimations[j++] == false)
                    continue;
                
                for (int i = 0; i < dataAnimation.Sprites.Count; i++)
                {
                    var orig = data.Original.SubTexture[i];
                    var s = new SpriteMetaData();
                    var actualY = totalHeight - orig.y - orig.height;

                    orig.SpriteName = s.name = string.Format("{0}_{1}_{2}", data.FileName, dataAnimation.NewName, i);
                    s.pivot = new Vector2(0.5f, 0.5f);
                    s.rect = new Rect(orig.x, actualY, orig.width, orig.height);
                    sprites.Add(s);


                }
            }
            importer.spritesheet = sprites.ToArray();
            importer.SaveAndReimport();

            return assetFilePath;
        }

        public static string GetProjectPathFromFullPath(string dstPath)
        {
            dstPath = dstPath.Replace('\\', '/');
            var toLoad = "Assets" + dstPath.Replace(Application.dataPath, string.Empty);
            return toLoad;
        }

        public static void GetImageSize(TextureImporter importer, out int width, out int height)
        {
            object[] args = new object[2] { 0, 0 };
            MethodInfo mi = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
            mi.Invoke(importer, args);

            width = (int)args[0];
            height = (int)args[1];
        }

        public static List<AnimationClip> GenerateAnimations(string animationClipsDestination, string assetPath, bool[] selectedAnimations,
           DetailedTextureInfo data, int framerate)
        {
            var allSprites = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var ret = new List<AnimationClip>();
            for (int i = 0; i < data.Animations.Count; i++)
            {
                if (selectedAnimations[i] == false)
                    continue;

                var animSprites = allSprites.Where(s => data.Animations[i].Sprites.Any(s2 => s2.SpriteName == s.name))
                    .OrderBy(s => s.name).ToList();

                var animClip = CreateAnimationClip(data.Animations[i], animSprites, animationClipsDestination, framerate, data);
                ret.Add(animClip);
            }

            return ret;
        }

        public static AnimationClip CreateAnimationClip(DBAnimation dataAnimation, List<Object> animSprites,
            string animationClipsDestination, int framerate, DetailedTextureInfo data)
        {
            var animationClipName = string.Format("{0}/{1}_{2}{3}",
                DataLoader.GetProjectPathFromFullPath(animationClipsDestination), data.FileName, dataAnimation.NewName, ".anim");

            var origClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationClipName);


            var animationClip = origClip ?? new AnimationClip();
            animationClip.name = dataAnimation.NewName;
            animationClip.frameRate = framerate;
            EditorCurveBinding curveBinding = new EditorCurveBinding
            {
                // I want to change the sprites of the sprite renderer, so I put the typeof(SpriteRenderer) as the binding type.
                type = typeof(SpriteRenderer),
                // Regular path to the gameobject that will be changed (empty string means root)
                path = "",
                // This is the property name to change the sprite of a sprite renderer

                propertyName = "m_Sprite"
            };
            var keyFrames = new List<ObjectReferenceKeyframe>();
            for (int j = 0; j < animSprites.Count; j++)
            {
                var frame = new ObjectReferenceKeyframe
                {
                    value = animSprites[j],
                    time = j / animationClip.frameRate
                };
                keyFrames.Add(frame);
            }

            AnimationUtility.SetAnimationClipSettings(animationClip, new AnimationClipSettings
            {
                loopTime = true
            });

            AnimationUtility.SetObjectReferenceCurve(animationClip, curveBinding, keyFrames.ToArray());

            if (origClip == null)
                AssetDatabase.CreateAsset(animationClip, animationClipName);

            AssetDatabase.SaveAssets();
            return animationClip;
        }

        public static AnimatorController GenerateAnimationController(string assetPath, DetailedTextureInfo data, string animationClipsDestination, List<AnimationClip> clips)
        {
            var controllerName = string.Format("{0}/{1}{2}", GetProjectPathFromFullPath(animationClipsDestination), data.FileName, ".controller");

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerName);

            if (controller != null)
            {
                Debug.LogError("AnimatorController Already exists, skipping its creation\n" + controllerName);
                return controller;
            }

            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerName);

            var rootStateMachine = controller.layers[0].stateMachine;

            AnimatorState entry = null;
            foreach (var animationClip in clips)
            {
                entry = rootStateMachine.AddState(animationClip.name);
                entry.motion = animationClip;
            }

            return controller;
        }
    }
}
