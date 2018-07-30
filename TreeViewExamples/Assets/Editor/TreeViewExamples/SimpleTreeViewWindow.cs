using UnityEngine;
using UnityEditor.IMGUI.Controls;


namespace UnityEditor.TreeViewExamples
{
	class SimpleTreeViewWindow : EditorWindow
	{
		// We are using SerializeField here to make sure view state is written to the window 
		// layout file. This means that the state survives restarting Unity as long as the window
		// is not closed. If omitting the attribute then the state just survives assembly reloading 
		// (i.e. it still gets serialized/deserialized)
		[SerializeField] TreeViewState m_TreeViewState;

		// The TreeView is not serializable it should be reconstructed from the tree data.
		SimpleTreeView m_TreeView;

		// SearchField: unity自带类
		SearchField m_SearchField;

		void OnEnable ()
		{
			// Check if we already had a serialized view state (state 
			// that survived assembly reloading)
			if (m_TreeViewState == null)
				m_TreeViewState = new TreeViewState ();

			m_TreeView = new SimpleTreeView(m_TreeViewState);
			m_SearchField = new SearchField ();

			// 在搜索框，按下上下键，选中TreeView
			m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;
		}

		void OnGUI ()
		{
			DoToolbar ();
			DoTreeView ();
		}

		// 显示搜索框
		void DoToolbar()
		{
			GUILayout.BeginHorizontal (EditorStyles.toolbar);
			GUILayout.Space (100);
			GUILayout.FlexibleSpace();
			// 设置TreeView的搜索框
			m_TreeView.searchString = m_SearchField.OnToolbarGUI (m_TreeView.searchString);
			GUILayout.EndHorizontal();
		}

		// 显示TreeView
		void DoTreeView()
		{
			Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
			m_TreeView.OnGUI(rect);
		}

		// Add menu named "My Window" to the Window menu
		[MenuItem ("TreeView Examples/Simple Tree Window")]
		static void ShowWindow ()
		{
			// Get existing open window or if none, make a new one:
			var window = GetWindow<SimpleTreeViewWindow> ();
			window.titleContent = new GUIContent ("My Window");
			window.Show ();
		}
	}
}
