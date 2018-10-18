using System;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEngine;


namespace UnityEditor.TreeViewExamples
{

	class MultiColumnWindow : EditorWindow
	{
		[NonSerialized] bool m_Initialized;
		[SerializeField] TreeViewState m_TreeViewState; // Serialized in the window layout file so it survives assembly reloading
		[SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState;

		// 搜索栏
		SearchField m_SearchField;
		// TreeView
		MultiColumnTreeView m_TreeView;

		MyTreeAsset m_MyTreeAsset;

		[MenuItem("TreeView Examples/Multi Columns")]
		public static MultiColumnWindow GetWindow ()
		{
			var window = GetWindow<MultiColumnWindow>();
			window.titleContent = new GUIContent("Multi Columns");
			window.Focus();
			window.Repaint();
			return window;
		}

		[OnOpenAsset]
		public static bool OnOpenAsset (int instanceID, int line)
		{
			var myTreeAsset = EditorUtility.InstanceIDToObject (instanceID) as MyTreeAsset;
			if (myTreeAsset != null)
			{
				var window = GetWindow ();
				window.SetTreeAsset(myTreeAsset);
				return true;
			}
			return false; // we did not handle the open
		}

		void SetTreeAsset (MyTreeAsset myTreeAsset)
		{
			m_MyTreeAsset = myTreeAsset;
			m_Initialized = false;
		}
		

		// -------------------------
		//		自定义排版的方法
		// -------------------------

		// TreeView显示区域
		Rect multiColumnTreeViewRect
		{
			get { return new Rect(20, 30, position.width-40, position.height-60); }
		}

		// 搜索栏，显示区域
		Rect toolbarRect
		{
			get { return new Rect (20f, 10f, position.width-40f, 20f); }
		}

		// 下部工具栏，显示区域
		Rect bottomToolbarRect
		{
			get { return new Rect(20f, position.height - 18f, position.width - 40f, 16f); }
		}

		public MultiColumnTreeView treeView
		{
			get { return m_TreeView; }
		}

		void InitIfNeeded ()
		{
			if (!m_Initialized)
			{
				// Check if it already exists (deserialized from window layout file or scriptable object)
				if (m_TreeViewState == null)
					m_TreeViewState = new TreeViewState();

				bool firstInit = m_MultiColumnHeaderState == null;

				// headerState: MultiColumnHeaderState结构
				var headerState = MultiColumnTreeView.CreateDefaultMultiColumnHeaderState(multiColumnTreeViewRect.width);

				// 检查，是否有序列化的MultiColumnHeaderState数据，可以使用
				// CanOverwriteSerializedFields:
				// Checks if the source state can transfer its serialized data to the destination state.
				// Some of the fields in the MultiColumnHeader state are serializable so they can be preserved
				// between assembly reloads and/or when restarting Unity.
				// The non-serialized fields have to be reconstructed.
				// After having reconstructed the state use this method before calling OverwriteSerializedFields to apply
				// any serialized fields from a previous session.
				if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
					MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);
				m_MultiColumnHeaderState = headerState;
				
				// 根据MultiColumnHeaderState结构
				// 创建MultiColumnHeader结构
				var multiColumnHeader = new MyMultiColumnHeader(headerState);
				if (firstInit)
					multiColumnHeader.ResizeToFit ();

				var treeModel = new TreeModel<MyTreeElement>(GetData());
				
				m_TreeView = new MultiColumnTreeView(m_TreeViewState, multiColumnHeader, treeModel);

				m_SearchField = new SearchField();
				m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;

				m_Initialized = true;
			}
		}
		
		// 从m_MyTreeAsset中获取数据
		// 如果没有m_MyTreeAsset，将生成随机数据
		IList<MyTreeElement> GetData ()
		{
			if (m_MyTreeAsset != null && m_MyTreeAsset.treeElements != null && m_MyTreeAsset.treeElements.Count > 0)
				return m_MyTreeAsset.treeElements;

			// generate some test data
			return MyTreeElementGenerator.GenerateRandomTree(130); 
		}

		// 如果在编辑器中，
		// 选择发生了变化
		// 如果选择了MyTreeAsset，则重新加载数据
		void OnSelectionChange ()
		{
			if (!m_Initialized)
				return;

			// 获得选中的数据
			var myTreeAsset = Selection.activeObject as MyTreeAsset;
			if (myTreeAsset != null && myTreeAsset != m_MyTreeAsset)
			{
				m_MyTreeAsset = myTreeAsset;
				m_TreeView.treeModel.SetData (GetData ());
				m_TreeView.Reload ();
			}
		}

		void OnGUI ()
		{
			InitIfNeeded();

			SearchBar (toolbarRect);
			DoTreeView (multiColumnTreeViewRect);
			BottomToolBar (bottomToolbarRect);
		}

		void SearchBar (Rect rect)
		{
			treeView.searchString = m_SearchField.OnGUI (rect, treeView.searchString);
		}

		void DoTreeView (Rect rect)
		{
			m_TreeView.OnGUI(rect);
		}

		// 显示下部按钮
		void BottomToolBar (Rect rect)
		{
			GUILayout.BeginArea (rect);

			using (new EditorGUILayout.HorizontalScope ())
			{

				var style = "miniButton";
				// 显示，展开所有按钮
				if (GUILayout.Button("Expand All", style))
				{
					treeView.ExpandAll ();
				}

				// 显示，关闭所有按钮
				if (GUILayout.Button("Collapse All", style))
				{
					treeView.CollapseAll ();
				}

				GUILayout.FlexibleSpace();

				// 显示，m_MyTreeAsset路径
				GUILayout.Label (m_MyTreeAsset != null ? AssetDatabase.GetAssetPath (m_MyTreeAsset) : string.Empty);

				GUILayout.FlexibleSpace ();

				// 显示，"Set sorting"按钮
				if (GUILayout.Button("Set sorting", style))
				{
					var myColumnHeader = (MyMultiColumnHeader)treeView.multiColumnHeader;
					myColumnHeader.SetSortingColumns (new int[] {4, 3, 2}, new[] {true, false, true});
					myColumnHeader.mode = MyMultiColumnHeader.Mode.LargeHeader;
				}


				GUILayout.Label ("Header: ", "minilabel");
				if (GUILayout.Button("Large", style))
				{
					var myColumnHeader = (MyMultiColumnHeader) treeView.multiColumnHeader;
					myColumnHeader.mode = MyMultiColumnHeader.Mode.LargeHeader;
				}
				if (GUILayout.Button("Default", style))
				{
					var myColumnHeader = (MyMultiColumnHeader)treeView.multiColumnHeader;
					myColumnHeader.mode = MyMultiColumnHeader.Mode.DefaultHeader;
				}
				if (GUILayout.Button("No sort", style))
				{
					var myColumnHeader = (MyMultiColumnHeader)treeView.multiColumnHeader;
					myColumnHeader.mode = MyMultiColumnHeader.Mode.MinimumHeaderWithoutSorting;
				}

				GUILayout.Space (10);
				
				// 显示数值，还是显示控件
				if (GUILayout.Button("values <-> controls", style))
				{
					treeView.showControls = !treeView.showControls;
				}
			}

			GUILayout.EndArea();
		}
	}


	internal class MyMultiColumnHeader : MultiColumnHeader
	{
		Mode m_Mode;

		public enum Mode
		{
			LargeHeader,
			DefaultHeader,
			MinimumHeaderWithoutSorting
		}

		public MyMultiColumnHeader(MultiColumnHeaderState state)
			: base(state)
		{
			mode = Mode.DefaultHeader;
		}

		public Mode mode
		{
			get
			{
				return m_Mode;
			}
			set
			{
				m_Mode = value;
				switch (m_Mode)
				{
					case Mode.LargeHeader:
						canSort = true;
						height = 37f;
						break;
					case Mode.DefaultHeader:
						canSort = true;
						height = DefaultGUI.defaultHeight;
						break;
					case Mode.MinimumHeaderWithoutSorting:
						canSort = false;
						height = DefaultGUI.minimumHeight;
						break;
				}
			}
		}

		protected override void ColumnHeaderGUI (MultiColumnHeaderState.Column column, Rect headerRect, int columnIndex)
		{
			// Default column header gui
			base.ColumnHeaderGUI(column, headerRect, columnIndex);

			// Add additional info for large header
			if (mode == Mode.LargeHeader)
			{
				// Show example overlay stuff on some of the columns
				if (columnIndex > 2)
				{
					headerRect.xMax -= 3f;
					var oldAlignment = EditorStyles.largeLabel.alignment;
					EditorStyles.largeLabel.alignment = TextAnchor.UpperRight;
					GUI.Label(headerRect, 36 + columnIndex + "%", EditorStyles.largeLabel);
					EditorStyles.largeLabel.alignment = oldAlignment;
				}
			}
		}
	}

}
