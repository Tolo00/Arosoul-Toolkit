using UnityEngine;
using Alchemy.Inspector;


#if UNITY_EDITOR
using UnityEditorInternal;
#endif

public enum ItemType { Simple, Example }

[DisallowMultipleComponent]
public class Item : MonoBehaviour
{
    public ItemData Data;
    [OnValueChanged("OnItemTypeChanged")] public ItemType Type;

    [Space]
    
    [SerializeField, HideInInspector] private Component _itemBehaviour;

    void Awake()
    {
        Data = Instantiate(Data);
    }

    private void OnItemTypeChanged()
    {
        // Check for same component
        bool sameComp = false;
        if (Type == ItemType.Simple && _itemBehaviour == null) sameComp = true;
        else if (Type == ItemType.Example && _itemBehaviour is ExampleItemBehaviour) sameComp = true;

        if (sameComp) return;

        // Replace component
        if (_itemBehaviour != null) DestroyImmediate(_itemBehaviour); 

        if (Type == ItemType.Example)
            _itemBehaviour = gameObject.AddComponent<ExampleItemBehaviour>();
        
        #if UNITY_EDITOR
        PlaceAfter(_itemBehaviour, this);
        #endif
    }

#if UNITY_EDITOR

    public static void PlaceAfter(Component componentToMove, Component referenceComponent)
    {
        if (componentToMove == null || referenceComponent == null) return;
        if (componentToMove.gameObject != referenceComponent.gameObject) return;

        Component[] components = componentToMove.GetComponents<Component>();

        int targetIndex = System.Array.IndexOf(components, referenceComponent) + 1;
        int currentIndex = System.Array.IndexOf(components, componentToMove);

        while (currentIndex < targetIndex)
        {
            ComponentUtility.MoveComponentDown(componentToMove);
            currentIndex++;
        }

        while (currentIndex > targetIndex)
        {
            ComponentUtility.MoveComponentUp(componentToMove);
            currentIndex--;
        }
    }

#endif
}
