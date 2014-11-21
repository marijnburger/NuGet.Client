﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Packaging
{
    public class TreeBuilder
    {
        private readonly List<Tuple<TreeItem, List<TreeProperty>>> _items;

        public TreeBuilder()
        {
            _items = new List<Tuple<TreeItem, List<TreeProperty>>>();
        }

        public void Add(TreeItem item, IEnumerable<TreeProperty> properties)
        {
            _items.Add(new Tuple<TreeItem, List<TreeProperty>>(item, properties.ToList()));
        }

        public ComponentTree GetTree()
        {
            var workingItems = new List<Tuple<TreeItem, List<TreeProperty>>>(_items);

            var rootItems = GetRootItems(workingItems).ToArray();

            workingItems.RemoveAll(t => rootItems.Contains(t));

            TreeNode root = new TreeNode(rootItems.Select(t => t.Item1), Enumerable.Empty<TreeProperty>(), BuildChildren(workingItems));

            return new ComponentTree(root);
        }

        private static IEnumerable<TreeNode> BuildChildren(IEnumerable<Tuple<TreeItem, List<TreeProperty>>> remainingItems)
        {
            string pivotKey = GetBestPivot(remainingItems);

            // TODO: handle items that should go under the default
            var levelItems = remainingItems.Where(t => t.Item2.Any(p => StringComparer.Ordinal.Equals(pivotKey, p.PivotKey)));

            if (remainingItems.Count() != levelItems.Count())
            {
                throw new NotImplementedException("unable to handle this type of tree");
            }

            var grouped = new Dictionary<TreeProperty, List<Tuple<TreeItem, List<TreeProperty>>>>();

            foreach (var item in levelItems)
            {
                // TODO: handle when items have multiple properties of the same key type
                var pivotProp = item.Item2.Where(p => StringComparer.Ordinal.Equals(pivotKey, p.PivotKey)).Single();

                List<Tuple<TreeItem, List<TreeProperty>>> val = null;

                if (!grouped.TryGetValue(pivotProp, out val))
                {
                    val = new List<Tuple<TreeItem, List<TreeProperty>>>();
                    grouped.Add(pivotProp, val);
                }

                val.Add(item);

                item.Item2.Remove(pivotProp);
            }

            foreach (var pivotProp in grouped.Keys)
            {
                // items with no properties go in this node
                var nodeItems = grouped[pivotProp].Where(t => !t.Item2.Any()).Select(t => t.Item1);
                var nodeProps = new TreeProperty[] { pivotProp };

                // items that still have properties go into children
                var nextLevel = grouped[pivotProp].Where(t => t.Item2.Any()).ToArray();

                IEnumerable<TreeNode> nodeChildren = BuildChildren(nextLevel);

                yield return new TreeNode(nodeItems, nodeProps, nodeChildren);
            }

            yield break;
        }

        // special case - root items
        private static IEnumerable<Tuple<TreeItem, List<TreeProperty>>> GetRootItems(IEnumerable<Tuple<TreeItem, List<TreeProperty>>> items)
        {
            foreach (var item in items)
            {
                if (item.Item2.Count == 1)
                {
                    KeyValueTreeProperty kvProp = item.Item2[0] as KeyValueTreeProperty;

                    if (kvProp != null && StringComparer.Ordinal.Equals(kvProp.Key, PackagingConstants.TargetFrameworkPropertyKey)
                        && StringComparer.Ordinal.Equals(kvProp.Value, PackagingConstants.AnyFramework))
                    {
                        yield return item;
                    }
                }
            }

            yield break;
        }

        // larget group of pivots
        private static string GetBestPivot(IEnumerable<Tuple<TreeItem, List<TreeProperty>>> items)
        {
            var pivotGroups = items.SelectMany(t => t.Item2).GroupBy(p => p.PivotKey)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.First().PivotKey, StringComparer.Ordinal);

            if (pivotGroups.Any())
            {
                return pivotGroups.FirstOrDefault().Select(g => g.PivotKey).FirstOrDefault();
            }

            return null;
        }

    }
}
