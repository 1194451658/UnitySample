using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;


namespace UnityEditor.TreeViewExamples
{
	// The TreeModel is a utility class working on a list of serializable TreeElements where the order and the depth of each TreeElement define
	// the tree structure. Note that the TreeModel itself is not serializable (in Unity we are currently limited to serializing lists/arrays) but the 
	// input list is.
	// The tree representation (parent and children references) are then build internally using TreeElementUtility.ListToTree (using depth 
	// values of the elements). 
	// The first element of the input list is required to have depth == -1 (the hiddenroot) and the rest to have
	// depth >= 0 (otherwise an exception will be thrown)

	public class TreeModel<T> where T : TreeElement
	{
		IList<T> m_Data;
		T m_Root;
		int m_MaxID;
	
		public T root { get { return m_Root; } set { m_Root = value; } }
		public event Action modelChanged;
		public int numberOfDataElements
		{
			get { return m_Data.Count; }
		}

		public TreeModel (IList<T> data)
		{
			SetData (data);
		}

		public T Find (int id)
		{
			return m_Data.FirstOrDefault (element => element.id == id);
		}
	
		public void SetData (IList<T> data)
		{
			Init (data);
		}

		void Init (IList<T> data)
		{
			if (data == null)
				throw new ArgumentNullException("data", "Input data is null. Ensure input is a non-null list.");

			m_Data = data;
			if (m_Data.Count > 0)
				m_Root = TreeElementUtility.ListToTree(data);

			m_MaxID = m_Data.Max(e => e.id);
		}

		public int GenerateUniqueID ()
		{
			return ++m_MaxID;
		}

		// 得到node为id的，所有的父亲节点
		public IList<int> GetAncestors (int id)
		{
			var parents = new List<int>();
			TreeElement T = Find(id);
			if (T != null)
			{
				while (T.parent != null)
				{
					parents.Add(T.parent.id);
					T = T.parent;
				}
			}
			return parents;
		}

		// 得到id节点下面，有孩子的节点
		public IList<int> GetDescendantsThatHaveChildren (int id)
		{
			T searchFromThis = Find(id);
			if (searchFromThis != null)
			{
				return GetParentsBelowStackBased(searchFromThis);
			}
			return new List<int>();
		}

		// 得到searchFromThis下面，有孩子的节点（初始检查包括searchFromThis）
		// 是以stack来实现的
		IList<int> GetParentsBelowStackBased(TreeElement searchFromThis)
		{
			Stack<TreeElement> stack = new Stack<TreeElement>();
			stack.Push(searchFromThis);

			var parentsBelow = new List<int>();
			while (stack.Count > 0)
			{
				TreeElement current = stack.Pop();
				if (current.hasChildren)
				{
					parentsBelow.Add(current.id);
					foreach (var T in current.children)
					{
						stack.Push(T);
					}
				}
			}

			return parentsBelow;
		}

		// 移除node
		public void RemoveElements (IList<int> elementIDs)
		{
			IList<T> elements = m_Data.Where (element => elementIDs.Contains (element.id)).ToArray ();
			RemoveElements (elements);
		}

		// 移除node
		public void RemoveElements (IList<T> elements)
		{
			foreach (var element in elements)
				if (element == m_Root)
					throw new ArgumentException("It is not allowed to remove the root element");
		
			// 删除elements中，是其他节点的后代节点的节点
			// 返回的结果是新的list
			// （结果只保留顶级的父亲节点）
			var commonAncestors = TreeElementUtility.FindCommonAncestorsWithinList (elements);

			// 移除这些剩下的顶级节点
			// (这样做的，是为了避免重复的删除节点；
			// 如果删除了顶级节点，则在顶级节点下面的节点，就不用再删除了，因为已经随顶级节点被删除)
			foreach (var element in commonAncestors)
			{
				element.parent.children.Remove (element);
				element.parent = null;
			}

			// 树结构改变后，重新更新m_data
			TreeElementUtility.TreeToList(m_Root, m_Data);

			// 通知数据改变
			Changed();
		}

		// 将节点element，
		// 加入到parent的，insertPosition位置起始
		public void AddElements (IList<T> elements, TreeElement parent, int insertPosition)
		{
			if (elements == null)
				throw new ArgumentNullException("elements", "elements is null");
			if (elements.Count == 0)
				throw new ArgumentNullException("elements", "elements Count is 0: nothing to add");
			if (parent == null)
				throw new ArgumentNullException("parent", "parent is null");

			if (parent.children == null)
				parent.children = new List<TreeElement>();

			parent.children.InsertRange(insertPosition, elements.Cast<TreeElement> ());
			foreach (var element in elements)
			{
				element.parent = parent;
				element.depth = parent.depth + 1;
				// 更新root下面的孩子节点们的深度
				TreeElementUtility.UpdateDepthValues(element);
			}

			// 树结构改变后，重新更新m_data
			TreeElementUtility.TreeToList(m_Root, m_Data);

			// 通知数据改变
			Changed();
		}

		// 添加根节点root
		// （必须没有其他的节点）
		public void AddRoot (T root)
		{
			if (root == null)
				throw new ArgumentNullException("root", "root is null");

			if (m_Data == null)
				throw new InvalidOperationException("Internal Error: data list is null");

			if (m_Data.Count != 0)
				throw new InvalidOperationException("AddRoot is only allowed on empty data list");

			root.id = GenerateUniqueID ();
			root.depth = -1;
			m_Data.Add (root);
		}

		// 添加节点element，到parent
		public void AddElement (T element, TreeElement parent, int insertPosition)
		{
			if (element == null)
				throw new ArgumentNullException("element", "element is null");
			if (parent == null)
				throw new ArgumentNullException("parent", "parent is null");
		
			if (parent.children == null)
				parent.children = new List<TreeElement> ();

			parent.children.Insert (insertPosition, element);
			element.parent = parent;

			TreeElementUtility.UpdateDepthValues(parent);
			TreeElementUtility.TreeToList(m_Root, m_Data);

			Changed ();
		}

		// 将elements，移动到parentElement的insertionIndex开始的地方
		public void MoveElements(TreeElement parentElement, int insertionIndex, List<TreeElement> elements)
		{
			if (insertionIndex < 0)
				throw new ArgumentException("Invalid input: insertionIndex is -1, client needs to decide what index elements should be reparented at");

			// Invalid reparenting input
			if (parentElement == null)
				return;

			// We are moving items so we adjust the insertion index to accomodate that any items above the insertion index is removed before inserting
			// 检查有elements中，有多少元素，已经在新的parentElement下面了
			// 这样在移动的时候，实际的insertionIndex应该进行更新
			if (insertionIndex > 0)
				insertionIndex -= parentElement.children.GetRange(0, insertionIndex).Count(elements.Contains);

			// Remove draggedItems from their parents
			foreach (var draggedItem in elements)
			{
				draggedItem.parent.children.Remove(draggedItem);	// remove from old parent
				draggedItem.parent = parentElement;					// set new parent
			} 

			if (parentElement.children == null)
				parentElement.children = new List<TreeElement>();

			// Insert dragged items under new parent
			parentElement.children.InsertRange(insertionIndex, elements);

			// 更新depth
			TreeElementUtility.UpdateDepthValues (root);
			// 更新到m_data
			TreeElementUtility.TreeToList (m_Root, m_Data);

			// 通知数据更改
			Changed ();
		}

		void Changed ()
		{
			if (modelChanged != null)
				modelChanged ();
		}
	}


	#region Tests
	class TreeModelTests
	{
		[Test]
		public static void TestTreeModelCanAddElements()
		{
			var root = new TreeElement {name = "Root", depth = -1};
			var listOfElements = new List<TreeElement>();
			listOfElements.Add(root);

			var model = new TreeModel<TreeElement>(listOfElements);
			model.AddElement(new TreeElement { name = "Element"  }, root, 0);
			model.AddElement(new TreeElement { name = "Element " + root.children.Count }, root, 0);
			model.AddElement(new TreeElement { name = "Element " + root.children.Count }, root, 0);
			model.AddElement(new TreeElement { name = "Sub Element" }, root.children[1], 0);

			// Assert order is correct
			string[] namesInCorrectOrder = { "Root", "Element 2", "Element 1", "Sub Element", "Element" };
			Assert.AreEqual(namesInCorrectOrder.Length, listOfElements.Count, "Result count does not match");
			for (int i = 0; i < namesInCorrectOrder.Length; ++i)
				Assert.AreEqual(namesInCorrectOrder[i], listOfElements[i].name);

			// Assert depths are valid
			TreeElementUtility.ValidateDepthValues(listOfElements);
		}
	
		[Test]
		public static void TestTreeModelCanRemoveElements()
		{
			var root = new TreeElement { name = "Root", depth = -1 };
			var listOfElements = new List<TreeElement>();
			listOfElements.Add(root);

			var model = new TreeModel<TreeElement>(listOfElements);
			model.AddElement(new TreeElement { name = "Element"  }, root, 0);
			model.AddElement(new TreeElement { name = "Element " + root.children.Count }, root, 0);
			model.AddElement(new TreeElement { name = "Element " + root.children.Count }, root, 0);
			model.AddElement(new TreeElement { name = "Sub Element" }, root.children[1], 0);

			model.RemoveElements(new[] { root.children[1].children[0], root.children[1] });

			// Assert order is correct
			string[] namesInCorrectOrder = { "Root", "Element 2", "Element" };
			Assert.AreEqual(namesInCorrectOrder.Length, listOfElements.Count, "Result count does not match");
			for (int i = 0; i < namesInCorrectOrder.Length; ++i)
				Assert.AreEqual(namesInCorrectOrder[i], listOfElements[i].name);

			// Assert depths are valid
			TreeElementUtility.ValidateDepthValues(listOfElements);
		}
	}

	#endregion

}
