using Alchemy.Inspector;
using UnityEngine;

[CreateAssetMenu(fileName = "DebugContext", menuName = "Scriptable Objects/DebugContext")]
public class DebugContext : ScriptableObject
{
    private static DebugContext _inst;
    private static DebugContext Inst { 
        get
        {
            if (_inst == null)
            {
                var loaded = Resources.LoadAll<DebugContext>("");
                Debug.Assert(loaded.Length != 0, "Debug context could not be found.");
                Debug.Assert(loaded.Length <= 1, "Multiple objects of type DebugContext found.");
                _inst = loaded[0];
            }
            return _inst;
        }
    }

    [SerializeField] bool _developmentMode = false;
    [SerializeField, ShowIf("_developmentMode")] bool _debugObjectsEnabled = true;
    
    public static bool DevelopmentMode => Inst._developmentMode && Application.isEditor;
    public static bool DebugObjectsEnabled => DevelopmentMode && Inst._debugObjectsEnabled;
}
