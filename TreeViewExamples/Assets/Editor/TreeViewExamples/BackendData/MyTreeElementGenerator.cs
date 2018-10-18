using System.Collections.Generic;
using Random = UnityEngine.Random;


namespace UnityEditor.TreeViewExamples
{

	static class MyTreeElementGenerator
	{
		static int IDCounter;
		static int minNumChildren = 5;
		static int maxNumChildren = 10;
		static float probabilityOfBeingLeaf = 0.5f;

		public static List<MyTreeElement> GenerateRandomTree(int numTotalElements)
		{
			// 这里是随机创建树
			// numRootChildren: 在根节点上调用几次AddChildrenRecursive
			int numRootChildren = numTotalElements / 4;
			IDCounter = 0;
			var treeElements = new List<MyTreeElement>(numTotalElements);

			// name, depth, id
			var root = new MyTreeElement("Root", -1, IDCounter);
			treeElements.Add(root);
			for (int i = 0; i < numRootChildren; ++i)
			{
				int allowedDepth = 6;
				AddChildrenRecursive(root, Random.Range(minNumChildren, maxNumChildren), true, numTotalElements, ref allowedDepth, treeElements);
			}

			return treeElements;
		}

		// force:
		//	* false，会判断概率，设置成叶节点；也就是，可能不会创建numTotalElements个孩子
		//	* true，不会概率判断，是否能成为叶节点；也就是，会强制添加numTotalElements个孩子
		static void AddChildrenRecursive( TreeElement element,
			 int numChildren,
			 bool force,
			 int numTotalElements,					// 允许创建的孩子个数
			 ref int allowedDepth,					// 允许的最大深度
			 List<MyTreeElement> treeElements)
		{
			if (element.depth >= allowedDepth)
			{
				allowedDepth = 0;
				return;
			}

			for (int i = 0; i < numChildren; ++i)
			{
				// 检查是否超出总个数
				if (IDCounter > numTotalElements)
					return;

				var child = new MyTreeElement("Element " + IDCounter, element.depth + 1, ++IDCounter);
				treeElements.Add(child);

				if (!force && Random.value < probabilityOfBeingLeaf)
					continue;

				AddChildrenRecursive(child, Random.Range(minNumChildren, maxNumChildren), false, numTotalElements, ref allowedDepth, treeElements);
			}
		}
	}
}
