using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inventory
{
    [CreateAssetMenu]
    public class InventorySo : ScriptableObject
    {
        [SerializeField] private List<InventoryItem> inventoryItem;
        [field: SerializeField] public int Size { get; private set; } = 10;
        public event Action<Dictionary<int, InventoryItem>> OnInventoryUpdated;


        #region Methods
        
        /// <summary>
        /// Initialize the inventory data.
        /// </summary>
        public void Initialize()
        {
            inventoryItem = new List<InventoryItem>();
            for (int i = 0; i < Size; i++)
                inventoryItem.Add(InventoryItem.GetEmptyItem());
        }
        
        
        /// <summary>
        /// Add an item to the inventory.
        /// </summary>
        /// <param name="item">Item to add.</param>
        /// <param name="quantity">Amount of added item.</param>
        public void AddItem(ItemSo item, int quantity)
        {
            for (int i = 0; i < inventoryItem.Count; i++)
            {
                if (!inventoryItem[i].IsEmpty) continue;
                inventoryItem[i] = new InventoryItem { item = item, quantity = quantity };
                return;
            }
        }
        
        public void AddItem(InventoryItem item)
        {
            AddItem(item.item, item.quantity);
        }
   
        
        /// <summary>
        /// Get the current inventory state (What items and how many item in the inventory).
        /// </summary>
        /// <returns>Current inventory state and data.</returns>
        public Dictionary<int, InventoryItem> GetCurrentInventoryState()
        {
            Dictionary<int, InventoryItem> returnValue = new Dictionary<int, InventoryItem>();
            
            for (int i = 0; i < inventoryItem.Count; i++)
            {
                if (inventoryItem[i].IsEmpty) continue;
                returnValue[i] = inventoryItem[i];
            }
            
            return returnValue;
        }
    
        
        /// <summary>
        /// Get the item from the inventory at the given index.
        /// </summary>
        /// <param name="itemIndex">Index to get.</param>
        /// <returns>Item at the given index.</returns>
        public InventoryItem GetItemAt(int itemIndex)
        {
            return inventoryItem[itemIndex];
        }

        
        /// <summary>
        /// Swap 2 items in the inventory (Include moving one item to the empty slot).
        /// </summary>
        /// <param name="itemIndex1">Item one</param>
        /// <param name="itemIndex2">Item two</param>
        public void SwapItems(int itemIndex1, int itemIndex2)
        {
            Debug.Log($"{itemIndex1},{itemIndex2}");
            (inventoryItem[itemIndex1], inventoryItem[itemIndex2]) = (inventoryItem[itemIndex2], inventoryItem[itemIndex1]);
            InformAboutChange();
        }
        
        
        /// <summary>
        /// Update the inventory data.
        /// </summary>
        private void InformAboutChange()
        {
            OnInventoryUpdated?.Invoke(GetCurrentInventoryState());
        }
        #endregion
    }
    
    [Serializable]
    public struct InventoryItem
    {
        public int quantity;
        public ItemSo item;
        public bool IsEmpty => item == null;
        
        public InventoryItem ChangeQuantity(int newQuantity)
        {
            return new InventoryItem { item = this.item, quantity = newQuantity };
        }
        
        public static InventoryItem GetEmptyItem()
        => new InventoryItem { item = null, quantity = 0, };
    }
}

