using Godot;
using System;

public class Main : Node2D
{
	[Export]
	private int gr_scale_x = 600;
	[Export]
	private int gr_scale_y = 10;
	[Export]
	private int[] fr_on_graph;
	[Export]
	private int[] fr_samples;
	[Export]
	private Vector2 default_fr_range = new Vector2(20, 20000);
	[Export]
	private String working_dir = OS.GetSystemDir(OS.SystemDir.Desktop)+"/EQappData";
	public String last_name;

	private Font font = ResourceLoader.Load<Font>("res://font.tres");
	private PackedScene file_dialog = ResourceLoader.Load<PackedScene>("res://FileDialog.tscn");
	private PackedScene file_dialog_item = ResourceLoader.Load<PackedScene>("res://FileListItem.tscn");
	private PackedScene ExternalFileDialog = ResourceLoader.Load<PackedScene>("res://ExternalFileDialog.tscn");
	private PackedScene legendItem = ResourceLoader.Load<PackedScene>("res://LegendItem.tscn");
	private PackedScene MusicPlayer = ResourceLoader.Load<PackedScene>("res://MusicPlayer.tscn");
	private AudioEffectEQ EQ = new AudioEffectEQ();

	private Vector2[] points;
	private float[] graph_x;
	private int[] graph_y;
	private Vector2 mouse_pos = new Vector2(0, 0);
	public int frequency = 0;
	public float decibels = 0.0f;
	private Vector2[] target;
	private Godot.Collections.Dictionary<Color, Vector2[]> comparison_graphs = new Godot.Collections.Dictionary<Color, Vector2[]>();
	private Godot.Collections.Dictionary target_fr = new Godot.Collections.Dictionary();
	private Godot.Collections.Dictionary<int, float> eq_data = new Godot.Collections.Dictionary<int, float>();
	private Godot.Collections.Array<Godot.Collections.Dictionary<int, float>> eq_data_aux = new Godot.Collections.Array<Godot.Collections.Dictionary<int, float>>();
	private const float zoom_spd = .05f;
	private Vector2 pos_mouse;
	private Vector2 pos_cam;
	private Node db_node;
	private bool active = true;

	private enum FileMenu
	{
		IMPORT,
		EXPORT,
		OPEN_WD,
	}
	private String[] file_str = {"Import GraphicEQ", "Export GraphicEQ from current graph", "Open working directory"};

	private enum GraphMenu
	{
		LOAD,
		SAVE,
		SAVE_AS,
		ADD_COMP,
	}
	private String[] graph_str = {"Load", "Save", "Save as...", "Add comparison graph"};

	private enum GenItems
	{
		ADJUST,
		FROM_FILES,
		FROM_FILES_CSV,
	}
	private String[] gen_str = {"Adjust FR to target FR", "Generate EQ from files", "Generate EQ from files - CSV"};

	private enum ToolsMenuItems
	{
		LOAD_IMG,
	}
	private String[] tools_str = {"Load background image"};

	private enum OptionsMenuItems
	{
		SET_TARGET,
		SET_FREQUENCIES,
		SET_SAMPLES,
	}
	private String[] options_str = {"Set FR target", "Change graph frequencies", "Set frequency samples"};

	private static void sort(Godot.Collections.Array arr)
	{
		for (int i = 0; i < arr.Count-1; i++)
		{
			for (int j = i+1; j < arr.Count; j++)
			{
				Vector2 p1 = (Vector2)arr[i];
				Vector2 p2 = (Vector2)arr[j];
				if (p1.x>p2.x)
				{
					Vector2 aux = (Vector2)arr[i];
					arr[i] = arr[j];
					arr[j] = aux;
				}
			}
		}
	}

	private static void to_v2arr(Godot.Collections.Array arr, Vector2[] conv)
	{
		conv = new Vector2[arr.Count];
		for (int i = 0; i < arr.Count; i++)
		{
			conv[i] = (Vector2)arr[i];
		}
	}

	private void status(String text)
	{
		Godot.Collections.Dictionary datetime = OS.GetDatetime();
		String datetime_str =
		(
			"\n["+(datetime["day"])+"/"+(datetime["month"])+"/"+(datetime["year"])+" - "
			+ (datetime["hour"])+":"+(datetime["minute"])+":"+(datetime["second"])+"]  "
		);
		RichTextLabel l = GetParent().GetNode("UI/Status") as RichTextLabel;
		l.Text += datetime_str + text;
	}

	private Vector2 get_graph_point(Vector2 point)
	{
		return new Vector2((float)Math.Log10(point.x)*gr_scale_x-730.618f, (170.0f-point.y)*gr_scale_y-400.0f);
	}

	private int has_point(Godot.Collections.Array arr, float x)
	{
		int index = 0;
		foreach (Vector2 i in arr)
		{
			if (i.x == x)
			{
				return index;
			}
			index++;
		}
		return -1;
	}

	private void add_target_value()
	{
		Godot.Collections.Array aux = new Godot.Collections.Array(target);
		Vector2 point = get_graph_point(new Vector2(frequency, decibels));
		int replace = has_point(aux, point.x);
		if (replace != -1)
		{
			
			if (target[replace].y!=point.y)
			{
				target[replace] = point;
				status("FR graph updated ("+(frequency).ToString()+"Hz -> "+(decibels).ToString()+"dB)");
				eq_data[frequency] = decibels;
				((AudioEffectEQ)AudioServer.GetBusEffect(1, 0)).SetBandGainDb(frequency, decibels-90.0f);
			}
			// else
			// {
			// 	aux.Add(point);
			// 	sort(aux);
			// 	to_v2arr(aux, target);
			// 	eq_data[frequency] = decibels;
			// }
		}
	}

	private void update_graph_form_eqdata()
	{
		target = new Vector2[eq_data.Count];
		int j = 0;
		foreach (int i in eq_data.Keys)
		{
			target[j] = get_graph_point(new Vector2(i, (float)eq_data[i]));
			j++;
		}
		Update();
	}

	private void update_comparison_graphs()
	{
		VBoxContainer list =GetParent().GetNode<VBoxContainer>("UI/Legend/List");
		int nr = list.GetChildCount();
		
		for (int k = 2; k<nr; k++)
		{
			Godot.Collections.Dictionary g = (Godot.Collections.Dictionary)list.GetChild<Panel>(k).Get("data_graph");
			Vector2[] points = new Vector2[g.Count];
			int j = 0;
			foreach (int i in g.Keys)
			{
				points[j] = get_graph_point(new Vector2(i, (float)g[i]));
				j++;
			}
			list.GetChild<Panel>(k).Set("data_points", points);
		}
	}

	private void gen_eq_files()
	{
		File f = new File();
		if (f.FileExists(working_dir+"\\GraphicEQt.txt") && 
			f.FileExists(working_dir+"\\GraphicEQs.txt")   )
		{
			f.Open(working_dir+"\\GraphicEQt.txt", File.ModeFlags.Read);
			String g1 = "";
			while(!f.EofReached())
			{
				String line = f.GetLine();
				g1 += line;
			}
			f.Close();
			if (!g1.BeginsWith("GraphicEQ:"))
			{
				status("Invalid GraphicEQ file - GraphicEQt.txt");
				return;
			}
			g1 = g1.Replace("GraphicEQ:", "");
			String[] g1_sep = g1.Split(";", false);
			Godot.Collections.Dictionary<int, float> g1d = new Godot.Collections.Dictionary<int, float>();
			foreach(String i in g1_sep)
			{
				String[] s = i.Split(" ", false);
				g1d[(int)s[0].ToFloat()] = s[1].ToFloat();
			}

			f.Open(working_dir+"\\GraphicEQs.txt", File.ModeFlags.Read);
			String g2 = "";
			while (!f.EofReached())
			{
				String line = f.GetLine();
				g2+=line;
			}
			f.Close();
			if (!g2.BeginsWith("GraphicEQ:"))
			{
				status("Invalid GraphicEQ file - GraphicEQs.txt");
				return;
			}
			g2 = g2.Replace("GraphicEQ:", "");
			String[] g2_sep = g2.Split(";", false);
			Godot.Collections.Dictionary<int, float> g2d = new Godot.Collections.Dictionary<int, float>();
			foreach(String i in g2_sep)
			{
				String[] s = i.Split(" ", false);
				g2d[(int)s[0].ToFloat()] = s[1].ToFloat();
			}

			String data = "GraphicEQ: ";
			eq_data.Clear();
			f.Open(working_dir+"\\GraphicEQ_result.txt", File.ModeFlags.Write);
			foreach (int i in g1d.Keys)
			{
				eq_data[i] = 90.0f + (g1d[i] - g2d[i]);
				data += i.ToString() + " " + (eq_data[i] - 100.0f).ToString() + "; ";
			}
			f.StoreString(data);
			f.Close();

			update_graph_form_eqdata();

			status("Exported current FR graph to GraphicEQ_result.txt");

		}
		else
		{
			status("Missing GraphicEQt.txt or GraphicEQs.txt");
		}
	}

	private void gen_eq_files_csv()
	{
		File f = new File();
		if (f.FileExists(working_dir+"\\GraphicEQt.txt") && 
			f.FileExists(working_dir+"\\GraphicEQs.txt")   )
		{
			f.Open(working_dir+"\\GraphicEQt.txt", File.ModeFlags.Read);
			String g1 = "";
			while(!f.EofReached())
			{
				String line = f.GetLine();
				g1 += line+"_";
			}
			f.Close();
			if (!g1.BeginsWith("frequency,raw"))
			{
				status("Invalid GraphicEQ file - Desltop\\GraphicEQt.txt");
				return;
			}
			g1 = g1.Replace("frequency,raw", "");
			String[] g1_sep = g1.Split("_", false);
			Godot.Collections.Dictionary<int, float> g1d = new Godot.Collections.Dictionary<int, float>();
			foreach(String i in g1_sep)
			{
				String[] s = i.Split(",", false);
				g1d[(int)s[0].ToFloat()] = s[1].ToFloat();
			}

			f.Open(working_dir+"\\GraphicEQs.txt", File.ModeFlags.Read);
			String g2 = "";
			while (!f.EofReached())
			{
				String line = f.GetLine();
				g2+=line+"_";
			}
			f.Close();
			if (!g2.BeginsWith("frequency,raw"))
			{
				status("Invalid GraphicEQ file - GraphicEQs.txt");
				return;
			}
			g2 = g2.Replace("frequency,raw", "");
			String[] g2_sep = g2.Split("_", false);
			Godot.Collections.Dictionary<int, float> g2d = new Godot.Collections.Dictionary<int, float>();
			foreach(String i in g2_sep)
			{
				String[] s = i.Split(",", false);
				g2d[(int)s[0].ToFloat()] = s[1].ToFloat();
			}

			String data = "GraphicEQ: ";
			eq_data.Clear();
			f.Open(working_dir+"\\GraphicEQ_result.txt", File.ModeFlags.Write);
			foreach (int i in g1d.Keys)
			{
				eq_data[i] = 90.0f + (g1d[i] - g2d[i]);
				data += i.ToString() + " " + (eq_data[i] - 100.0f).ToString() + "; ";
			}
			f.StoreString(data);
			f.Close();

			update_graph_form_eqdata();

			status("Exported current FR graph to GraphicEQ_result.txt");

		}
		else
		{
			status("Missing GraphicEQt.txt or GraphicEQs.txt");
		}
	}

	public void save_graph()
	{
		export_graphiceq("user://temp/tempdata.txt");
		Control fd = file_dialog.Instance<Control>();
		VBoxContainer fl = fd.GetNode<VBoxContainer>("WindowDialog/Files/FileList");
		Control parent = GetParent<Control>();
		Godot.Collections.Array items = (Godot.Collections.Array)db_node.Call("GetEntries");

		foreach (Godot.Collections.Array i in items)
		{
			Button item = (Button)file_dialog_item.Instance();
			Label text = item.GetNode<Label>("Name");
			Label descr = item.GetNode<Label>("Description");
			text.Text = (String)i[0];
			descr.Text = (String)i[1];
			fl.AddChild(item);
		}

		parent.AddChild(fd);
		WindowDialog window = fd.GetNode<WindowDialog>("WindowDialog");
		window.PopupCentered();
	}

	public void load_graph(String func)
	{
		Control fd = file_dialog.Instance<Control>();
		VBoxContainer fl = fd.GetNode<VBoxContainer>("WindowDialog/Files/FileList");
		Control parent = GetParent<Control>();
		Godot.Collections.Array items = (Godot.Collections.Array)db_node.Call("GetEntries");

		foreach (Godot.Collections.Array i in items)
		{
			Button item = (Button)file_dialog_item.Instance();
			Label text = item.GetNode<Label>("Name");
			Label descr = item.GetNode<Label>("Description");
			item.Connect("pressed", db_node, "DownloadGraph", new Godot.Collections.Array{i[2], func});
			text.Text = (String)i[0];
			descr.Text = (String)i[1];
			fl.AddChild(item);
		}

		parent.AddChild(fd);
		WindowDialog window = fd.GetNode<WindowDialog>("WindowDialog");
		window.GetNode<Button>("Save").Visible = false;
		window.GetNode<Button>("Cancel").Visible = false;
		window.GetNode<ScrollContainer>("Files").MarginBottom = 0;
		window.GetNode<LineEdit>("Name").Visible = false;
		window.GetNode<LineEdit>("Description").Visible = false;
		window.PopupCentered();
	}

	public void add_comparison_graph(String dir)
	{
		File f = new File();
		if (f.FileExists(dir))
		{
			f.Open(dir, File.ModeFlags.Read);
			String g1 = "";
			while(!f.EofReached())
			{
				String line = f.GetLine();
				g1 += line+"_";
			}
			f.Close();
			Godot.Collections.Dictionary<int, float> g1d = new Godot.Collections.Dictionary<int, float>();
			if (g1.BeginsWith("GraphicEQ: "))
			{
				g1 = g1.Replace("_", "").Replace("GraphicEQ:", "");
				String[] g1_sep = g1.Split(";", false);
				
				foreach(String i in g1_sep)
				{
					String[] s = i.Split(" ", false);
					if (s.Length == 2)
					{
						g1d[(int)s[0].ToFloat()] = s[1].ToFloat();
					}
					
				}
				status("Loaded graph from GraphicEQ file "+dir);
			}
			else
			{
				OS.Alert("Tried to load an invalid file "+dir);
				return;
			}

			Godot.Collections.Dictionary<int, float> eq = new Godot.Collections.Dictionary<int, float>();
			foreach (int i in g1d.Keys)
			{
				eq[i] = 90.0f + (g1d[i]);
			}
			eq_data_aux.Add(eq);

			RandomNumberGenerator rng = new RandomNumberGenerator();
			rng.Randomize();
			Color c = Color.Color8((byte)(0+rng.RandiRange(0, 255)), (byte)(0+rng.RandiRange(0, 255)), (byte)(0+rng.RandiRange(0, 255)));
			Panel lItem = legendItem.Instance<Panel>();
			lItem.GetNode<ColorRect>("Color").Color = c;
			lItem.GetNode<Label>("Name").Text = last_name;
			GetParent().GetNode<VBoxContainer>("UI/Legend/List").AddChild(lItem);
			lItem.Set("data_graph", eq);
			update_comparison_graphs();
			Update();
		}
		else
		{
			OS.Alert("Missing file "+dir);
		}
	}

	public void import_graphiceq(String dir)
	{
		File f = new File();
		if (f.FileExists(dir))
		{
			f.Open(dir, File.ModeFlags.Read);
			String g1 = "";
			while(!f.EofReached())
			{
				String line = f.GetLine();
				g1 += line+"_";
			}
			f.Close();
			Godot.Collections.Dictionary<int, float> g1d = new Godot.Collections.Dictionary<int, float>();
			if (g1.BeginsWith("frequency,raw"))
			{
				g1 = g1.Replace("frequency,raw", "");
				String[] g1_sep = g1.Split("_", false);
				
				foreach(String i in g1_sep)
				{
					String[] s = i.Split(",", false);
					g1d[(int)s[0].ToFloat()] = s[1].ToFloat();
				}
				status("Loaded graph from csv file "+dir);
			}
			else
			{
				if (g1.BeginsWith("GraphicEQ: "))
				{
					g1 = g1.Replace("_", "").Replace("GraphicEQ:", "");
					String[] g1_sep = g1.Split(";", false);
					
					foreach(String i in g1_sep)
					{
						String[] s = i.Split(" ", false);
						if (s.Length == 2)
						{
							g1d[(int)s[0].ToFloat()] = s[1].ToFloat();
						}
						
					}
					status("Loaded graph from GraphicEQ file "+dir);
				}
				else
				{
					OS.Alert("Tried to load an invalid file "+dir);
					return;
				}
			}

			eq_data.Clear();
			foreach (int i in g1d.Keys)
			{
				eq_data[i] = 90.0f + (g1d[i]);
			}
			update_graph_form_eqdata();
		}
		else
		{
			OS.Alert("Missing file "+dir);
		}
	}

	public void export_graphiceq(String dir)
	{
		String data = "GraphicEQ: ";
		File file = new File();
		file.Open(dir, File.ModeFlags.Write);
		foreach(int i in eq_data.Keys)
		{
			data+=i.ToString() + " " + Math.Round(eq_data[i] - 90.0f, 1).ToString()+"; ";
		}
		file.StoreString(data);
		file.Close();
		status("Exported current FR graph to "+dir);
	}

	public void option_file(int id)
	{
		switch(id)
		{
			case((int)FileMenu.IMPORT):
				Control efd = ExternalFileDialog.Instance<Control>();
				AddChild(efd);
				efd.Call("Initialize", working_dir);
				break;
			case((int)FileMenu.EXPORT):
				export_graphiceq(working_dir+"/GraphicEQ.txt");
				break;
			case((int)FileMenu.OPEN_WD):
				OS.ShellOpen(working_dir);
				status("Opened working directory in File Manager");
				break;
		}
	}

	public void option_graph(int id)
	{
		switch(id)
		{
			case((int)GraphMenu.LOAD):
				load_graph("import_graphiceq");
				break;
			case((int)GraphMenu.SAVE):
				save_graph();
				break;
			case((int)GraphMenu.SAVE_AS):
				save_graph();
				break;
			case(int)GraphMenu.ADD_COMP:
				load_graph("add_comparison_graph");
				break;
		}
	}

	public void option_geneq(int id)
	{
		switch(id)
		{
			case((int)GenItems.ADJUST):
				if (target_fr.Count == 0)
				{
					status("FR target not set");
				}
				else
				{
					foreach (int i in eq_data.Keys)
					{
						eq_data[i] = 90.0f + ((float)target_fr[i] - (float)eq_data[i]);
					}
					update_graph_form_eqdata();
					status("Updated current FR graph to match target FR");
				}
				break;
			case((int)GenItems.FROM_FILES):
				gen_eq_files();
				break;
			case((int)GenItems.FROM_FILES_CSV):
				gen_eq_files_csv();
				break;
		}
	}

	public void option_tools(int id)
	{
		switch(id)
		{
			case((int)ToolsMenuItems.LOAD_IMG):
				File f = new File();
				String path = working_dir+"\\img.png";
				if (f.FileExists(path))
				{
					Image img = new Image();
					if (img.Load(path) != Error.Ok)
					{
						status("Failed to load image - img.png");
						return;
					}
					ImageTexture texture = new ImageTexture();
					texture.CreateFromImage(img);
					TextureRect bkg = (TextureRect)GetNode("bkg");
					bkg.Texture = texture;
					status("Background image loaded");
				}
				else
				{
					status("Image not found - img.png");
				}
				break;
		}
	}

	public void option_options(int id)
	{
		switch(id)
		{
			case((int)OptionsMenuItems.SET_FREQUENCIES):
				status("[Not implemented] Set frequencies");
				break;
			case((int)OptionsMenuItems.SET_SAMPLES):
				status("[Not implemented] Set frequency samples");
				break;
			case((int)OptionsMenuItems.SET_TARGET):
				File file = new File();
				file.Open("user://target.txt", File.ModeFlags.Write);
				file.StoreVar(eq_data, true);
				file.Close();
				
				status("Target FR updated, saved as target.txt");
				load_target();
				break;
		}
	}

	private void initialize_menubar()
	{
		foreach (MenuButton i in GetParent().GetNode("UI/MenuBar").GetChildren())
		{
			i.GetPopup().AddFontOverride("font", font);
		}

		foreach (String i in file_str)
		{
			MenuButton m = (MenuButton)GetParent().GetNode("UI/MenuBar/File");
			m.GetPopup().AddItem(i);
		}

		foreach (String i in graph_str)
		{
			MenuButton m = (MenuButton)GetParent().GetNode("UI/MenuBar/Graph");
			m.GetPopup().AddItem(i);
		}

		foreach (String i in options_str)
		{
			MenuButton m = (MenuButton)GetParent().GetNode("UI/MenuBar/Options");
			m.GetPopup().AddItem(i);
		}

		foreach (String i in tools_str)
		{
			MenuButton m = (MenuButton)GetParent().GetNode("UI/MenuBar/Tools");
			m.GetPopup().AddItem(i);
		}

		foreach (String i in gen_str)
		{
			MenuButton m = (MenuButton)GetParent().GetNode("UI/MenuBar/GenerateEQ");
			m.GetPopup().AddItem(i);
		}

		MenuButton mn = (MenuButton)GetParent().GetNode("UI/MenuBar/GenerateEQ");
		mn.GetPopup().Connect("id_pressed", this, "option_geneq");
		mn = (MenuButton)GetParent().GetNode("UI/MenuBar/Tools");
		mn.GetPopup().Connect("id_pressed", this, "option_tools");
		mn = (MenuButton)GetParent().GetNode("UI/MenuBar/Options");
		mn.GetPopup().Connect("id_pressed", this, "option_options");
		mn = (MenuButton)GetParent().GetNode("UI/MenuBar/File");
		mn.GetPopup().Connect("id_pressed", this, "option_file");
		mn = (MenuButton)GetParent().GetNode("UI/MenuBar/Graph");
		mn.GetPopup().Connect("id_pressed", this, "option_graph");

		status("Initialized MenuBar");
	}

	public void clear()
	{
		target = new Vector2[fr_samples.Length];
		decibels = 90;
		eq_data.Clear();

		foreach(int i in fr_samples)
		{
			eq_data[i] = 90.0f;
			frequency = i;
			add_target_value();
		}

		update_graph_form_eqdata();

		RichTextLabel t = (RichTextLabel)GetParent().GetNode("UI/Status");
		t.Text = "";
		status("Graph cleared");
	}

	public void smoothen()
	{
		for (int i = 1; i < target.Length-1; i++)
		{
			Vector2 p1 = (Vector2)target[i-1];
			Vector2 p2 = (Vector2)target[i+1];
			Vector2 pm = (Vector2)target[i];
			pm.y = Mathf.Lerp(pm.y, (p1.y + p2.y)/2, .5f);
			eq_data[(Mathf.RoundToInt(Mathf.Pow(10, (pm.x+730.618f)/gr_scale_x)))] = Mathf.Stepify(170.0f-(pm.y+400.0f)/gr_scale_y, 0.1f);
		}

		update_graph_form_eqdata();
		
		status("Graph smoothened");
	}

	public void Amplify(float val)
	{
		if (val == 0.0f)
		{
			status("Already normalized");
			return;
		}
		if (Mathf.Abs(val) == 1)
		{
			if (Input.IsActionPressed("ctrl"))
			{
				val/=10.0f;
			}
		}
		foreach (int i in eq_data.Keys)
		{
			eq_data[i]+=val;
		}
		update_graph_form_eqdata();
		status("Amplified graph with "+val.ToString()+"dB");
	}

	public void Normalize()
	{
		float max = -99;
		foreach (int i in eq_data.Keys)
		{
			if(eq_data[i]>max)
			{
				max = eq_data[i];
			}
		}
		Amplify(90.0f-max);
	}

	private void auto_sample(int nr, Vector2 range)
	{
		range = new Vector2((float)Math.Log10(range.x), (float)Math.Log10(range.y));
		float s = (range.y - range.x)/nr;
		float i = range.x;
		fr_samples = new int[nr+1];

		int j = 0;
		while(i<range.y)
		{
			int f = (int)Mathf.Pow(10, i);
			fr_samples[j] = f;
			j++;
			i+=s;
		}

		status("Frequency samples updated (auto -> "+nr.ToString()+" samples)");
	}

	private void load_target()
	{
		File file = new File();
		if (file.FileExists("user://target.txt"))
		{
			file.Open("user://target.txt", File.ModeFlags.Read);
			target_fr = (Godot.Collections.Dictionary)file.GetVar(true);
			status("Target FR loaded");
		}
		else
		{
			status("No target FR detected");
		}
		file.Close();
	}

	private void initialize_graph()
	{
		//horizontal axis
		float offset = -(float)Math.Log10(fr_on_graph[0])*gr_scale_x+50;
		graph_x = new float[fr_on_graph.Length];
		int j = 0;
		foreach(int i in fr_on_graph)
		{
			float x = (float)Math.Log10(i)*gr_scale_x+offset;
			graph_x[j] = x;
			j++;
		}

		//vertical axis
		j = 50;
		offset = -40*gr_scale_y;
		graph_y = new int[((120-j)/5)];
		int k = 0;
		while(j<120)
		{
			graph_y[k] = j*gr_scale_y+(int)offset;
			k++;
			j+=5;
		}

		status("Initialized Graph");
	}

	private void initialize_buttons()
	{
		Control UI = (Control)GetParent().GetNode("UI");
		Button b_smoothen = (Button)UI.GetNode("Smoothen");
		Button b_clear = (Button)UI.GetNode("Clear");
		Button b_amp0 = UI.GetNode<Button>("Amplify/Amp-");
		Button b_amp1 = UI.GetNode<Button>("Amplify/Amp+");
		Button b_normalize = UI.GetNode<Button>("Normalize");

		b_smoothen.Connect("pressed", this, "smoothen");
		b_clear.Connect("pressed", this, "clear");
		b_amp0.Connect("pressed", this, "Amplify", new Godot.Collections.Array{-1.0f});
		b_amp1.Connect("pressed", this, "Amplify", new Godot.Collections.Array{ 1.0f});
		b_normalize.Connect("pressed", this, "Normalize");
	}

	public void set_active(bool a)
	{
		active = a;
	}
	
	public override void _Ready()
	{
		Directory d = new Directory();
		if (!d.DirExists(working_dir))
		{
			d.MakeDir(working_dir);
		}
		if (!d.DirExists("user://temp"))
		{
			d.MakeDir("user://temp");
		}

		ColorRect bkg = GetParent().GetNode<ColorRect>("ColorRect");
		bkg.Connect("mouse_entered", this, "set_active", new Godot.Collections.Array(){true});
		bkg.Connect("mouse_exited", this, "set_active", new Godot.Collections.Array(){false});

		db_node = (Node)GetNode("Database");
		db_node.Call("InitializeDatabase", working_dir);
		status("Initialized Database");
		auto_sample(695, default_fr_range);
		clear();
		auto_sample(695, default_fr_range);
		initialize_menubar();
		load_target();
		initialize_graph();
		initialize_buttons();

	}
	public override void _Process(float delta)
	{
		if (active)
		{
			//get mouse coordinates on graph
			mouse_pos = GetLocalMousePosition();
			mouse_pos.x = Mathf.Clamp(mouse_pos.x, 50, 1850);
			mouse_pos.y = Mathf.Clamp(mouse_pos.y, 100, 750);

			frequency = Mathf.RoundToInt(Mathf.Pow(10, (mouse_pos.x+730.618f)/gr_scale_x));

			for (int i = 0; i < fr_samples.Length-1; i++)
			{
				if (frequency <= fr_samples[i+1])
				{
					if ((fr_samples[i] + fr_samples[i+1])/2 - frequency > 0)
					{
						frequency = fr_samples[i];
					}
					else
					{
						frequency = fr_samples[i+1];
					}
					break;
				}
			}

			decibels = Mathf.Stepify(170-(mouse_pos.y+400)/gr_scale_y, 0.1f);

			//move/scale bkg image
			float x_move = Input.GetActionStrength("ui_right") - Input.GetActionStrength("ui_left");
			float y_move = Input.GetActionStrength("ui_down") - Input.GetActionRawStrength("ui_up");

			if (Input.IsActionPressed("ctrl"))
			{
				Vector2 incr = new Vector2(x_move, y_move);
				if (Input.IsActionPressed("lshift"))
				{
					incr/=10;
				}
				TextureRect bkg = (TextureRect)GetNode("bkg");
				bkg.RectPosition+=incr;
			}
			if (Input.IsActionPressed("alt"))
			{
				Vector2 incr = new Vector2(x_move, y_move);
				if (Input.IsActionPressed("lshift"))
				{
					incr/=10;
				}
				TextureRect bkg = (TextureRect)GetNode("bkg");
				bkg.RectSize+=incr;
			}

			//zoom graph
			int zoom = Convert.ToInt16(Input.IsActionJustReleased("wheel_down")) - Convert.ToInt16(Input.IsActionJustReleased("wheel_up"));
			Vector2 add_zoom = Vector2.One*zoom*zoom_spd*Scale;
			Scale -= add_zoom;
			Position += GetLocalMousePosition()*add_zoom;

			//move graph
			if (Input.IsActionJustPressed("mb_right"))
			{
				pos_mouse = GetGlobalMousePosition();
				pos_cam = Position;
			}

			if (Input.IsActionPressed("mb_right"))
			{
				Vector2 pos_new = pos_cam + GetGlobalMousePosition() - pos_mouse;
				//pos_mouse = pos_mouse + (pos_new - GlobalPosition);
				Position = pos_new;
			}


			//set point on graph
			if (Input.IsActionPressed("mb_left"))
			{
				if (mouse_pos == GetLocalMousePosition())
				{
					add_target_value();
				}
			}

			Update();
		}
	}

	public override void _Draw()
	{

		int j = 120;
		foreach (int i in graph_y)
		{
			DrawLine(new Vector2(graph_x[0], i), new Vector2(graph_x[0]+1800, i), Colors.DarkGray);
			DrawString(font, new Vector2(graph_x[0]-40, i), j.ToString());
			j-=5;
		}
		
		for (int i = 0; i < graph_x.Length; i++)
		{
			String s;
			if (fr_on_graph[i] >= 1000)
			{
				s = ((int)(fr_on_graph[i]/1000)).ToString()+"k";
			}
			else
			{
				s = fr_on_graph[i].ToString();
			}
			DrawLine(new Vector2(graph_x[i], 750), new Vector2(graph_x[i], 100), Colors.DarkSlateGray);
			DrawString(font, new Vector2(graph_x[i], 770), s);
		}

		if (mouse_pos == GetLocalMousePosition())
		{
			DrawLine(new Vector2(mouse_pos.x, 750), new Vector2(mouse_pos.x, 100), Colors.Yellow);
			DrawCircle(mouse_pos, 3, Colors.Blue);
			String fr = frequency.ToString()+"Hz";
			String db = decibels.ToString()+"dB";
			DrawString(font, new Vector2(mouse_pos.x, 50), fr);
			DrawString(font, new Vector2(mouse_pos.x, 80), db);
		}

		VBoxContainer list =GetParent().GetNode<VBoxContainer>("UI/Legend/List");
		int nr = list.GetChildCount();
		
		for (int k = 2; k<nr; k++)
		{
			Panel item = list.GetChild<Panel>(k);
			if ((bool)item.Get("visible") == true)
			{
				DrawPolyline((Vector2[])item.Get("data_points"), item.GetNode<ColorRect>("Color").Color, 1.5f);
			}
		}

		DrawPolyline(target, Colors.Yellow);

		foreach (Vector2 i in target)
		{
			DrawCircle(i, 1.0f, Colors.Yellow);
		}

	}
}
