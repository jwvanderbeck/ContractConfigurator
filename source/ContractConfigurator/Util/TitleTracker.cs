﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// Class for tracking and changing parameter titles in the stock contract app
    /// </summary>
    public class TitleTracker
    {
        private static GenericCascadingList cascadingList = null;
        private List<string> titles = new List<string>();

        /// <summary>
        /// Add a title to the TitleTracker to be tracked.  Call this in GetTitle() before returning a new string.
        /// </summary>
        /// <param name="title">The title to add</param>
        public void Add(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return;
            }
            titles.AddUnique(title);
        }

        /// <summary>
        /// Call this any time the title text has changed - this will make an attempt to update
        /// the contract window title.  We do this because otherwise the window will only ever read
        /// the title once.
        /// </summary>
        /// <param name="newTitle">New title to display</param>
        public void UpdateContractWindow(ContractParameter param, string newTitle)
        {
            // Try to find the cascading list in the contracts window.  Note that we may pick up
            // the ones from the Engineer's report in the VAB/SPH instead - but we don't care about
            // title updates in those scenes anyway.
            if (cascadingList == null || !cascadingList.gameObject.activeSelf)
            {
                cascadingList = UnityEngine.Object.FindObjectOfType<GenericCascadingList>();
            }

            // Every time the clock ticks over, make an attempt to update the contract window
            // title.  We do this because otherwise the window will only ever read the title once,
            // so this is the only way to get our fancy timer to work.

            // Go through all the list items in the contracts window
            if (cascadingList != null)
            {
                UIScrollList list = cascadingList.ruiList.cascadingList;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        // Try to find a rich text control that matches the expected text
                        UIListItemContainer listObject = (UIListItemContainer)list.GetItem(i);
                        SpriteTextRich richText = listObject.GetComponentInChildren<SpriteTextRich>();
                        if (richText != null)
                        {
                            // Check for any string in titleTracker
                            string found = null;
                            foreach (string title in titles)
                            {
                                if (richText.Text.Contains(title))
                                {
                                    found = title;
                                    break;
                                }
                            }

                            // Clear the titleTracker, and replace the text
                            if (found != null)
                            {
                                titles.Clear();
                                richText.Text = richText.Text.Replace(found, newTitle);
                                titles.Add(newTitle);
                            }
                        }
                    }

                    // Reposition items to account for items where the height increased or decreased
                    list.RepositionItems();
                }
            }

            // Contracts Window + update
            ContractsWindow.SetParameterTitle(param, newTitle);
        }
    }
}
