using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;
using ImaginationOverflow.DBSpriteSheetUnpacker.Editor.Data;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ImaginationOverflow.DBSpriteSheetUnpacker.Editor.Ui
{
    public class ImporterWindow : EditorWindow
    {
        private bool _prefsLoaded;
        private string _lastOpenLocation;
        private string _lastAnimClipSaveLocation;
        private string _lastSaveSpriteLocation;
        private int _framerate = 60;


        private DetailedTextureInfo _data;
        private bool[] _selectedAnimations;
        private string _spriteFolderPath;
        private bool _overrideSprite = true;
        private string _assetPath;
        private bool _generateAnimationController;
        private string _animationClipsDestination;

        [MenuItem("Window/ImaginationOverflow/SpriteSheetUnpacker")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ImporterWindow), true, "Dragon Bones Sprite Sheet Unpacker").Show();
        }

        private void LoadEditorPrefs()
        {
            if (_prefsLoaded)
                return;

            _prefsLoaded = true;

            _lastOpenLocation = EditorPrefs.GetString("_io_lastOpenLocation", string.Empty);
            _spriteFolderPath = _lastSaveSpriteLocation = EditorPrefs.GetString("_io_lastSaveSpriteLocation", Application.dataPath);
            _animationClipsDestination = _lastAnimClipSaveLocation = EditorPrefs.GetString("_io_lastAnimClipSaveLocation", Application.dataPath);
            _framerate = EditorPrefs.GetInt("_io_framerate", 60);





        }
        private void SavePrefs()
        {
            EditorPrefs.SetString("_io_lastOpenLocation", _lastOpenLocation);
            EditorPrefs.SetString("_io_lastSaveSpriteLocation", _lastSaveSpriteLocation);
            EditorPrefs.SetString("_io_lastAnimClipSaveLocation", _lastAnimClipSaveLocation);
            EditorPrefs.SetInt("_io_framerate", _framerate);
        }

        void OnGUI()
        {
            LoadEditorPrefs();
            if (GUILayout.Button("Load Dragon Bones file"))
            {

                //GenerateAnimations(
                //    @"D:/FAC/Programing/Projects/DragonBonesTextureUnpacker/DragonBonesTextureUnpacker/Assets/Resources/Animations",
                //    @"Assets/Resources/Sprites/Armature_tex.png",
                //    new bool[] { true, true },
                //    DataLoader.Load(@"C:\Users\DVDMSI\Desktop\New folder (3)\Armature_tex.json")
                //);

                //return;
                var filePath = EditorUtility.OpenFilePanel("Select a dragon bones json file", _lastOpenLocation, "json");
                _data = DataLoader.Load(filePath);

                SetupAnimations();

                _lastOpenLocation = Path.GetDirectoryName(filePath);
                SavePrefs();

                _assetPath = null;

            }
            else if (_data == null)
                return;


            DrawAnimationSelector();

            if (GUILayout.Button("Spritesheet Destination"))
            {
                _spriteFolderPath = _lastSaveSpriteLocation = EditorUtility.SaveFolderPanel("Pick Spritesheet Destination", _lastSaveSpriteLocation, "");
                SavePrefs();
            }

            if (string.IsNullOrEmpty(_spriteFolderPath))
                return;

            DrawSpriteOptions();


            /*
                EditorGUILayout.LabelField("  ");
            EditorGUILayout.LabelField("Type an URL or URI that you which to debug.");
            EditorGUILayout.LabelField("  ");
            */

            //const string SpritePath =
            //@"D:\FAC\Programing\Projects\DragonBonesTextureUnpacker\DragonBonesTextureUnpacker\Assets\Resources\Sprites";

            //const string AnimationDestinationPath =
            //    @"D:\FAC\Programing\Projects\DragonBonesTextureUnpacker\DragonBonesTextureUnpacker\Assets\Resources\Animation";

            bool overrideSprite = true;
            bool overrideAnimation = true;


            if (GUILayout.Button("Import"))
            {
                _assetPath = DataLoader.ImportSprite(_data, _spriteFolderPath, _overrideSprite, _selectedAnimations);

                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Sprite>(_assetPath);
            }

            if (string.IsNullOrEmpty(_assetPath))
                return;

            DrawAnimationOptions();

        }

        private void DrawAnimationOptions()
        {
            EditorGUILayout.LabelField("Animation Setup", EditorStyles.boldLabel);


            if (GUILayout.Button("Pick Animations Destination"))
            {
                _animationClipsDestination = _lastAnimClipSaveLocation = EditorUtility.SaveFolderPanel("Pick Animation Destination", _lastAnimClipSaveLocation, "");
                SavePrefs();
                _generateAnimationController = true;
            }

            if (string.IsNullOrEmpty(_animationClipsDestination))
                return;



            EditorGUILayout.LabelField(DataLoader.GetProjectPathFromFullPath(_animationClipsDestination), EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _generateAnimationController = EditorGUILayout.Toggle(_generateAnimationController, GUILayout.Width(20));
            EditorGUILayout.LabelField("Generate Animator Controller?", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Framerate", EditorStyles.boldLabel);
            _framerate = EditorGUILayout.IntField(_framerate);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Generate Animations"))
            {
                var clips = DataLoader.GenerateAnimations(_animationClipsDestination, _assetPath, _selectedAnimations, _data, _framerate);
                Object focus = clips.FirstOrDefault();
                if (_generateAnimationController)
                {
                    var controller = DataLoader.GenerateAnimationController(_assetPath, _data, _animationClipsDestination, clips);
                    focus = controller;
                }

                Selection.activeObject = focus;

            }

        }


        private void DrawSpriteOptions()
        {
            EditorGUILayout.LabelField(DataLoader.GetProjectPathFromFullPath(_spriteFolderPath), EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _overrideSprite = EditorGUILayout.Toggle(_overrideSprite, GUILayout.Width(20));
            EditorGUILayout.LabelField("Override Sprite?", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void SetupAnimations()
        {
            _selectedAnimations = new bool[_data.Animations.Count];
            for (int i = 0; i < _selectedAnimations.Length; i++)
            {
                _selectedAnimations[i] = true;
            }
        }

        private void DrawAnimationSelector()
        {
            EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            for (int i = 0; i < _data.Animations.Count; i++)
            {
                var anim = _data.Animations[i];
                _selectedAnimations[i] = EditorGUILayout.BeginToggleGroup(anim.Name, _selectedAnimations[i]);
                anim.NewName = EditorGUILayout.TextField("Name", anim.NewName);
                EditorGUILayout.LabelField(anim.Sprites.Count + " Frames");

                EditorGUILayout.EndToggleGroup();
            }

            EditorGUI.indentLevel--;
        }




    }
}
