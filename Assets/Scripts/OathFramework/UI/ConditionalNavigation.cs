using OathFramework.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OathFramework.UI
{

    [RequireComponent(typeof(Selectable))]
    public class ConditionalNavigation : LoopComponent, ILoopUpdate
    {
        public override int UpdateOrder => GameUpdateOrder.Default;

        [SerializeField] private Node[] nodes;
        private Selectable defaultOnUp;
        private Selectable defaultOnDown;
        private Selectable defaultOnLeft;
        private Selectable defaultOnRight;
        private Selectable selectable;

        private void Awake()
        {
            selectable     = GetComponent<Selectable>();
            defaultOnUp    = selectable.navigation.selectOnUp;
            defaultOnDown  = selectable.navigation.selectOnDown;
            defaultOnLeft  = selectable.navigation.selectOnLeft;
            defaultOnRight = selectable.navigation.selectOnRight;
        }

        void ILoopUpdate.LoopUpdate()
        {
            Navigation nav    = selectable.navigation;
            nav.mode          = Navigation.Mode.Explicit;
            nav.selectOnUp    = defaultOnUp;
            nav.selectOnDown  = defaultOnDown;
            nav.selectOnLeft  = defaultOnLeft;
            nav.selectOnRight = defaultOnRight;
            foreach(Node node in nodes) {
                if(node.GameObject == null || !node.GameObject.activeInHierarchy)
                    continue;

                nav.selectOnUp    = node.OnUp;
                nav.selectOnDown  = node.OnDown;
                nav.selectOnLeft  = node.OnLeft;
                nav.selectOnRight = node.OnRight;
                break;
            }
            selectable.navigation = nav;
        }
        
        [Serializable]
        private class Node
        {
            [field: SerializeField] public GameObject GameObject { get; private set; }
            [field: SerializeField] public Selectable OnUp       { get; private set; }
            [field: SerializeField] public Selectable OnDown     { get; private set; }
            [field: SerializeField] public Selectable OnLeft     { get; private set; }
            [field: SerializeField] public Selectable OnRight    { get; private set; }
        }
    }

}
