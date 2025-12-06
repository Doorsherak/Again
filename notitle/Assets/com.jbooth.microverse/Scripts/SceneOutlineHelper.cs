using System;
using System.Reflection;
using System.Threading.Tasks;

namespace JBooth.MicroVerseCore
{
    public static class SceneOutlineHelper
    {
        private static Assembly _unityEditorAssembly;
        private static Type _annotationUtilityType;
        private static PropertyInfo _sceneOutlineProperty;
        private static MethodInfo _sceneOutlineGetter;
        private static MethodInfo _sceneOutlineSetter;

        public static bool SceneOutline
        {
            get
            {
                // 1. null - its a static
                // 2. null - no parameters
                return (bool)SceneOutlineGetter.Invoke(null, null);
            }

            set
            {
                // 1. null - its a static
                // 2. value
                SceneOutlineSetter.Invoke(null, new object[] { value });
            }
        }

        #region DISABLE_WHILE_UPDATING

        // Why is this dumb and ugly timing thing here?
        // If we just immediately re-enable outlines after MV is done updating, we 100% run into situations like:
        // MV modifies -> disable outlines
        // MV is done -> enable outlines : this causes Unity to rebuild the scene renderer (according to the profiler probes)
        // a single frame passes
        // MV modifies again..... cause the user is dragging a height stamp or something

        // So instead lets just keep scene outlines disabled for a little bit, and wait until the scene settles

        private static bool _prevOutline = false;
        private static float _disableTime;
        private static float _tolerance = 0.05f;
        private static Task _enableTask = null;
        public static void DisableOutlines()
        {
            _prevOutline = SceneOutline;
            SceneOutline = false;
            _disableTime = _tolerance;
        }

        public static void EnableOutlines()
        {
            // Outlines already disabled no need to reenable
            if (!_prevOutline) return;

            if(_enableTask == null || _enableTask.IsCompleted)
                _enableTask = TimedEnable();
        }

        private static async Task TimedEnable()
        {
            while(_disableTime > 0)
            {
                await Task.Delay(10);

                _disableTime -= 0.01f;
            }

            SceneOutline = true;
        }
        #endregion

        #region REFLECTION
        private static MethodInfo SceneOutlineGetter
        {
            get
            {
                if (_sceneOutlineGetter == null)
                {
                    _sceneOutlineGetter = SceneOutlineProperty.GetGetMethod(nonPublic: true);
                    if (_sceneOutlineGetter == null)
                    {
                        throw new Exception("Failed to get the SceneOutline Getter for Scene Outline reflection.");
                    }
                }

                return _sceneOutlineGetter;
            }
        }

        private static MethodInfo SceneOutlineSetter
        {
            get
            {
                if(_sceneOutlineSetter == null)
                {
                    _sceneOutlineSetter = SceneOutlineProperty.GetSetMethod(nonPublic: true);
                    if(_sceneOutlineSetter == null)
                    {
                        throw new Exception("Failed to get the SceneOutline Setter for Scene Outline reflection.");
                    }
                }

                return _sceneOutlineSetter;
            }
        }

        private static PropertyInfo SceneOutlineProperty
        {
            get
            {
                if (_sceneOutlineProperty == null)
                {
                    _sceneOutlineProperty = AnnotationUtilityType.GetProperty(
                        "showSelectionOutline",
                        BindingFlags.Static | BindingFlags.NonPublic
                    );
                    if(_sceneOutlineProperty == null)
                    {
                        throw new Exception("Failed to get the SceneOutline Property for Scene Outline reflection.");
                    }
                }

                return _sceneOutlineProperty;
            }
        }

        private static Type AnnotationUtilityType
        {
            get
            {
                if(_annotationUtilityType == null)
                {
                    _annotationUtilityType = UnityEditorAssembly.GetType("UnityEditor.AnnotationUtility");
                    if(_annotationUtilityType == null)
                    {
                        throw new Exception("Failed to get the AnnotationUtility Type for Scene Outline reflection.");
                    }
                }

                return _annotationUtilityType;
            }
        }

        private static Assembly UnityEditorAssembly
        {
            get
            {
                if(_unityEditorAssembly == null)
                {
                    _unityEditorAssembly = Assembly.Load("UnityEditor");
                    if(_unityEditorAssembly == null)
                    {
                        throw new Exception("Failed to load the UnityEditor Assembly for Scene Outline reflection.");
                    }
                }

                return _unityEditorAssembly;
            }
        }
        #endregion
    }
}