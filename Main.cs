using Godot;
using System;

//Clasa Main care reprezintă de fapt graficul, moștenește Node2D (adică este un obiect 2D)
public class Main : Node2D
{
	//Variabilele cu [Export] pot fi setate și din interfața Godot

	//Parametrii pentru scalarea graficului (pentru a putea determina valorile în pixeli)
	//Am folosit valori fixe care se potrivesc pe orice monitor cu rezoluție full HD
	//Graficul oricum poate fi mărit sau micșorat din imaginea formată în funcția _Draw() în cazul în care nu arată corespunzător
	[Export]
	private int gr_scale_x = 600;
	[Export]
	private int gr_scale_y = 10;

	//Frecvențele afișate pe grafic ca referințe
	[Export]
	private int[] fr_on_graph;
	//Frecvențele graficului propriu-zis
	[Export]
	private int[] fr_samples;
	//Spectrul default de frecvențe
	[Export]
	private Vector2 default_fr_range = new Vector2(20, 20000);
	//Folderul în care lucrează programul
	[Export]
	private String working_dir = OS.GetSystemDir(OS.SystemDir.Desktop)+"/EQappData";

	//Numele ultimului grafic adăugat în lista de comparare a graficelor
	public String last_name;

	//Încărcarea în memorie a resurselor de bază
	private Font font = ResourceLoader.Load<Font>("res://font.tres");
	private PackedScene file_dialog = ResourceLoader.Load<PackedScene>("res://FileDialog.tscn");
	private PackedScene file_dialog_item = ResourceLoader.Load<PackedScene>("res://FileListItem.tscn");
	private PackedScene ExternalFileDialog = ResourceLoader.Load<PackedScene>("res://ExternalFileDialog.tscn");
	private PackedScene legendItem = ResourceLoader.Load<PackedScene>("res://LegendItem.tscn");
	private PackedScene MusicPlayer = ResourceLoader.Load<PackedScene>("res://MusicPlayer.tscn");

	//Punctele de pe grafic în pixeli
	private Vector2[] points;
	//Coordonata x a punctelor de pe grafic în Hz
	private float[] graph_x;
	//Coordonata y a punctelor de pe grafic în dB
	private int[] graph_y;
	//Variabilă care memorează poziția mouseului în interiorul graficului
	private Vector2 mouse_pos = new Vector2(0, 0);
	//Coordonata x în Hz a mouseului
	public int frequency = 0;
	//Coordonata y în dB a mouseului
	public float decibels = 0.0f;
	//Punctele în pixeli ale graficiului
	private Vector2[] target;
	//Lista graficelor adăugate pentru comparare (fiecare are câte o culoare și câte un vector cu punctele sale)
	private Godot.Collections.Dictionary<Color, Vector2[]> comparison_graphs = new Godot.Collections.Dictionary<Color, Vector2[]>();
	//Punctele în (Hz, dB) ale graficului țintă
	private Godot.Collections.Dictionary target_fr = new Godot.Collections.Dictionary();
	//Punctele în (Hz, dB) ale graficului curent
	private Godot.Collections.Dictionary<int, float> eq_data = new Godot.Collections.Dictionary<int, float>();
	//Grafice auxiliare în (Hz, dB) utilizate pentru diverse operații
	private Godot.Collections.Dictionary<int, float> eq_data_new = new Godot.Collections.Dictionary<int, float>();
	private Godot.Collections.Array<Godot.Collections.Dictionary<int, float>> eq_data_aux = new Godot.Collections.Array<Godot.Collections.Dictionary<int, float>>();
	//Variabile pentru mutarea/redimensionarea graficului
	private const float zoom_spd = .05f;
	private Vector2 pos_mouse;
	private Vector2 pos_cam;
	//Referință la baza de date
	private Node db_node;
	//Variabilă care reține dacă graficul este activ
	private bool active = true;
	//Variabilă care reține dacă se adaugă sau se scade un EQ din grafic
	private int subtract = 1;

	//Definirea butoanelor din Meniul din partea de sus
	private enum FileMenu
	{
		IMPORT,
		EXPORT,
		OPEN_WD,
	}
	private String[] file_str = {"Import GraphicEQ", "Export GraphicEQ", "Open working directory"};

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
		ADJUST_W,
		ADD_EQ,
		SUBTRACT_EQ,
	}
	private String[] gen_str = {"Adjust FR to target FR", "Adjust for Wavelet", "Add EQ", "Subtract EQ"};

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

	//Funcție pentru sortarea custom a unui vector de poziții 2D (se sortează după coordonata x a punctelor)
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

	//Convertirea unui Array Godot într-un vector de poziții 2D
	private static void to_v2arr(Godot.Collections.Array arr, Vector2[] conv)
	{
		conv = new Vector2[arr.Count];
		for (int i = 0; i < arr.Count; i++)
		{
			conv[i] = (Vector2)arr[i];
		}
	}

	//Programul are o consolă unde sunt afișate diverse mesaje referitoare la acțiunile utilizatorului
	//Această funcția adaugă un nou mesaj în consola respectivă
	//Mesajul va conține data și ora la care a fost produs
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

	//Convertirea unui punct de pe grafic din pixeli în (Hz, dB)
	private Vector2 get_graph_point(Vector2 point)
	{
		return new Vector2((float)Math.Log10(point.x)*gr_scale_x-730.618f, (170.0f-point.y)*gr_scale_y-400.0f);
	}

	//Găsirea locației unui punct de pe grafic în funcție de coordonata x
	//Nu ar trebui să existe 2 puncte cu aceeași coordonată x
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

	//Adăugarea unei valori pe grafic (se va face numai la coordonarele mouseului)
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
			}

		}
	}

	//Actualizarea graficului afișat în funcție de graficul în coordonate (Hz, dB)
	private void update_graph_form_eqdata()
	{
		target = new Vector2[eq_data.Count];
		fr_samples = new int[eq_data.Count];
		int j = 0;
		foreach (int i in eq_data.Keys)
		{
			target[j] = get_graph_point(new Vector2(i, (float)eq_data[i]));
			fr_samples[j] = i;
			j++;
		}
		Update();
	}

	//Actualizarea graficelor din lista de comparație
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

	//Salvarea graficului în baza de date
	public void save_graph()
	{
		//Se exportă graficul într-o locație temporară
		export_graphiceq("user://temp/tempdata.txt");
		//Se creează un FileDialog (care este realizat tot de mine)
		Control fd = file_dialog.Instance<Control>();
		//Referință la lista de fișiere
		VBoxContainer fl = fd.GetNode<VBoxContainer>("WindowDialog/Files/FileList");
		//Referință la nodul părinte (care e de fapt interfața grafică)
		Control parent = GetParent<Control>();
		//Se obțin toate graficele din baza de date
		Godot.Collections.Array items = (Godot.Collections.Array)db_node.Call("GetEntries");

		//Se creează intrări (butoane) pentru graficele obținute
		foreach (Godot.Collections.Array i in items)
		{
			Button item = (Button)file_dialog_item.Instance();
			Label text = item.GetNode<Label>("Name");
			Label descr = item.GetNode<Label>("Description");
			text.Text = (String)i[0];
			descr.Text = (String)i[1];
			fl.AddChild(item);
		}

		//Se adaugă la interfața grafică și se afișează fereastra cu FileDilog
		parent.AddChild(fd);
		WindowDialog window = fd.GetNode<WindowDialog>("WindowDialog");
		window.PopupCentered();
	}

	//Încărcarea unui grafic din baza de date
	//Am folosit același FileDialog ca la salvare numai că am ascuns elementele referitoare la salvarea unui grafic
	//Funcția primește ca argument numele funcției care se va executa la apasarea unui buton
	//	pentru a putea folosi FileDialog în mai multe moduri
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
			//Butoanele emit niște semnale care pot fi conectate la diverse funcții
			//Am conectat semnalul pressed funcția DownloadGraph și i-am dat ca parametrii numele funcției care va
			//	fi apelată la apăsarea butonului și id-ul graficului din baza de date pentru a știi cărui grafic corespunde butonul
			//În baza de date pot exista elemente cu aceeași denumrie, însă id-ul este unic
			item.Connect("pressed", db_node, "DownloadGraph", new Godot.Collections.Array{i[2], func});
			item.GetNode<Button>("Delete").Connect("pressed", db_node, "DeleteEntryId", new Godot.Collections.Array{i[2]});
			item.GetNode<Button>("Delete").Connect("pressed", this, "delete_db_entry", new Godot.Collections.Array{item});
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

	//Ștergerea din meniu a intrării din baza de date
	public void delete_db_entry(Button item)
	{
		item.QueueFree();
	}

	//Adăugarea unui grafic în lista de comparație
	//Graficul va fi luat dintr-o locație temporară (în urma descărcării acestuia din baza de date înainte de a fi apelată funcția)
	//Algoritmul și structura fișierelor vor fi explicate la funcția import_graphiceq()
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

			//Generarea unei culori pentru graficul de comparat
			RandomNumberGenerator rng = new RandomNumberGenerator();
			rng.Randomize();
			Color c = Color.Color8((byte)(0+rng.RandiRange(0, 255)), (byte)(0+rng.RandiRange(0, 255)), (byte)(0+rng.RandiRange(0, 255)));
			//Adăugarea graficului de comparat în lista de comparație
			Panel lItem = legendItem.Instance<Panel>();
			lItem.GetNode<ColorRect>("Color").Color = c;
			lItem.GetNode<LineEdit>("Name").Text = last_name;
			GetParent().GetNode<VBoxContainer>("UI/Legend/List").AddChild(lItem);
			lItem.Set("data_graph", eq);
			//Actualizarea graficelor de comparație
			update_comparison_graphs();
			Update();
		}
		else
		{
			OS.Alert("Missing file "+dir);
		}
	}

	//Adăugarea/eliminarea unui egalizator la graficul curent
	public void add_eq(String dir)
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
				status("Added EQ to graph from GraphicEQ file "+dir);
			}
			else
			{
				OS.Alert("Tried to load an invalid file "+dir);
				return;
			}

			foreach (int i in g1d.Keys)
			{
				eq_data[i] = eq_data[i] + (g1d[i]) * subtract;
			}

			update_graph_form_eqdata();
		}
		else
		{
			OS.Alert("Missing file "+dir);
		}
	}

	//Încărcarea unui grafic din clipboard
	//Pentru a simplifica lucrurile se salvează graficul, apoi se folosește o funcție deja existentă pentru a-l încărca
	public void import_graphiceq_from_clipboard()
	{
		File f = new File();
		f.Open("user://clipboard.txt", File.ModeFlags.Write);
		f.StoreString(OS.Clipboard);
		f.Close();
		import_graphiceq("user://clipboard.txt");
	}

	//Încărcarea unui grafic dintr-un fișier extern (fișiere externe se află în folderul de lucru -> /External)
	//Utilizatorul poate adăuga fișiere în folderul respectiv pentru a le importa si a le salva în baza de date
	//Programul suportă două tipuri de fișiere:
	//	- fișiere excel (csv) care au pe prima coloană "frequency", pe a doua coloană "raw"
	//	- fișiere text (txt) care au în interior structura GraphicEQ: frecvență1 amplitudine1; frecvență2 amplitudine2; ... 
	//Aceste tipuri de fișiere sunt foarte des întâlnite în aplicațiile la egalizatoare de sunet
	public void import_graphiceq(String dir)
	{
		//Se încarcă fișierul în memorie (dacă acesta există)
		File f = new File();
		if (f.FileExists(dir))
		{
			f.Open(dir, File.ModeFlags.Read);
			String g1 = "";
			//Se citește fișierul ca text, liniile se separă prin _ dacă este cazul
			while(!f.EofReached())
			{
				String line = f.GetLine();
				g1 += line+"_";
			}
			f.Close();
			//Graficul care urmează a fi generat din prelucrarea fișierului
			Godot.Collections.Dictionary<int, float> g1d = new Godot.Collections.Dictionary<int, float>();
			//Se verifică dacă graficul este în format csv, dacă da se vor extrage valorile din el
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
				//Dacă nu este în format csv, se verifică dacă este text cu structura GraphicEQ
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
					//Dacă fișierul este invalid (nu respectă niciunul din cele 2 tipuri) se va afișa mesaj de eroare și se va încheia funcția
					OS.Alert("Tried to load an invalid file "+dir);
					return;
				}
			}
			//După ce s-a format graficul din fișier, se actualizează graficul curent
			eq_data.Clear();
			foreach (int i in g1d.Keys)
			{
				eq_data[i] = 90.0f + (g1d[i]);
			}
			update_graph_form_eqdata();
		}
		else
		{
			//Mesaj de eroare dacă fișierul nu a fost găsit
			OS.Alert("Missing file "+dir);
		}
	}

	//Exportarea graficului într-un fișier extern text cu structura GraphicEQ
	public void export_graphiceq(String dir)
	{
		String data = "GraphicEQ: ";
		File file = new File();
		file.Open(dir, File.ModeFlags.Write);
		foreach(int i in eq_data.Keys)
		{
			data+=i.ToString() + " " + Math.Round(eq_data[i] - 90.0f, 1).ToString()+"; ";
		}
		//Trebuie eliminat ; de la sfârșit
		data = data.Remove(data.Length - 2, 2);
		file.StoreString(data);
		file.Close();
		status("Exported current graph to "+dir);
	}

	//Exportarea graficlui direct în clipboard
	public void export_graphiceq_to_clipboard()
	{
		String data = "GraphicEQ: ";
		foreach(int i in eq_data.Keys)
		{
			data+=i.ToString() + " " + Math.Round(eq_data[i] - 90.0f, 1).ToString()+"; ";
		}
		data = data.Remove(data.Length - 2, 2);
		OS.Clipboard = data;
		status("Exported current graph to clipboard");
	}

	//Înlocuirea frecvențelor din grafic
	//Există aplicații care suportă doar un anumit set de frecvențe
	//Această funcție va adapta graficul la un alt set de frecvențe fără a modifica amplitudinea
	public void ChangeFrSamples()
	{
		int k = 0;
		int[] CurrentSamples = new int[eq_data.Count];
		float[] SampleDecibels = new float[eq_data.Count];
		int[] NewSamples = new int[eq_data_new.Count];
		
		//Se inițializează graficele auxiliare
		foreach (int i in eq_data.Keys)
		{
			CurrentSamples[k] = i;
			SampleDecibels[k] = eq_data[i];
			k++;
		}
		k = 0;
		foreach (int i in eq_data_new.Keys)
		{
			NewSamples[k] = i;
			k++;
		}

		int l = 1;

		//Construirea noului grafic cu setul de frecvențe ales în variabila eq_data_new
		for (int j = 0; j<NewSamples.Length; j++)
		{
			//Se sare peste valorile care sunt deja prezente în graficul curent, dar care nu se regăsesc în noul set de frecvențe
			while(NewSamples[j] >= CurrentSamples[l] && l<CurrentSamples.Length-1)
			{
				l+=1;
			}
			l--;
			//Dacă valoarea din setul curent corespunde cu valoarea din setul de frecvențe, valoarea ramâne neschimbată
			if (NewSamples[j] == CurrentSamples[l])
			{
				eq_data_new[NewSamples[j]] = SampleDecibels[l];
			}
			//Alftel se va atribui o valoare intermediară corespunzătoare celei din setul de frecvențe
			else
			{
				eq_data_new[NewSamples[j]] = (SampleDecibels[l] + SampleDecibels[l+1])/2.0f;
			}
			l++;
			
		}
		//Actualizarea graficului afișat
		eq_data = eq_data_new.Duplicate(true);
		update_graph_form_eqdata();
		status("Adjusted graph FR samples");
	}

	//Funcțiile care se execută la apăsarea butoanelor
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
			//Generarea unui filtru care aplicat peste graficul curent va rezulta graficul target setat de către utilizator
			//Nu am mai realizat o funcție separată
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
			case((int)GenItems.ADJUST_W):
				//Ajustarea graficul la setul de frecvente compatibil cu programul Wavelet
				//Am luat valorile dintr-un fișier text pe care l-am inclus în aplicație
				File f = new File();
				f.Open("res://wavelet_frequencies.txt", File.ModeFlags.Read);
				String data = f.GetAsText();
				f.Close();
				String[] values = data.Split(", ", false);
				eq_data_new.Clear();
				foreach (String i in values)
				{
					eq_data_new[i.ToInt()] = 0.0f;
				}
				
				ChangeFrSamples();
				//export_graphiceq(working_dir+"/GraphicEQ.txt");
				break;
			case(int)GenItems.ADD_EQ:
				subtract = 1;
				load_graph("add_eq");
				break;
			case(int)GenItems.SUBTRACT_EQ:
				subtract = -1;
				load_graph("add_eq");
				break;
		}
	}

	public void option_tools(int id)
	{
		switch(id)
		{
			case((int)ToolsMenuItems.LOAD_IMG):
				//Încărcarea unei imagini de backgorund care va fi afișată în spatele graficului pentru a fi folosită ca referință
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

	//Inițializarea meniului din partea de sus
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

	//Resetarea graficului
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

	//Netezirea graficului prin interpolare
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

	//Amplificarea graficului
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

	//Normalizarea graficului - valoarea maximă în amplitudine va fi 90dB
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

	//Schimbarea numarului de frecvențe din grafic cu valori atribuite automat astfel încât punctele să fie la dinstanțe egale
	//	unele față de altele pe axa x (în pixeli)
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

	//Încărcarea graficului target (acesta nu este salvat în baza de date)
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

	//Inițializarea graficului
	private void initialize_graph()
	{
		//Axa orizontală
		float offset = -(float)Math.Log10(fr_on_graph[0])*gr_scale_x+50;
		graph_x = new float[fr_on_graph.Length];
		int j = 0;
		foreach(int i in fr_on_graph)
		{
			float x = (float)Math.Log10(i)*gr_scale_x+offset;
			graph_x[j] = x;
			j++;
		}

		//Axa verticală
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

	//Inițializarea butoanleor din partea dreapta - jos
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

	//Activarea/dezactivarea graficului (de ex. se utilizează în cazul în care se accesează un meniu)
	public void set_active(bool a)
	{
		active = a;
	}
	
	//Orice obiect de tip Node2D are niște funcții predefinite
	//Funcția _Ready() se execută imediat după ce a fost creat obiectul
	public override void _Ready()
	{
		//Crearea folderelor necesare (dacă e cazul)
		Directory d = new Directory();
		if (!d.DirExists(working_dir))
		{
			d.MakeDir(working_dir);
		}
		if (!d.DirExists("user://temp"))
		{
			d.MakeDir("user://temp");
		}

		//Backgroundul graficului e folosit pentru a detecta dacă mouseul se află deasupra și în interiorul acestuia
		//Practic, dacă se deschide un meniu peste grafic, acesta va fi dezactivat, dacă meniul dispare, atunci graficul va fi reactivat
		ColorRect bkg = GetParent().GetNode<ColorRect>("ColorRect");
		bkg.Connect("mouse_entered", this, "set_active", new Godot.Collections.Array(){true});
		bkg.Connect("mouse_exited", this, "set_active", new Godot.Collections.Array(){false});

		//Inițializarea bazei de date
		db_node = (Node)GetNode("Database");
		db_node.Call("InitializeDatabase", working_dir);
		status("Initialized Database");

		//Generarea unui grafic cu 695 frecvențe (numărul acesta este des întâlnit)
		auto_sample(695, default_fr_range);
		clear();
		auto_sample(695, default_fr_range);
		initialize_menubar();
		load_target();
		initialize_graph();
		initialize_buttons();

	}

	//În mod normal funcția _Process() nu trebuie folostă la aplicații, însă fiind un proiect mai complex n-am mai avut suficient timp să folosesc
	//	eventurile
	//Funcția se execută de 60 de ori per secundă
	public override void _Process(float delta)
	{
		if (active)
		{
			//Obținerea coordonatelor mouselului pe grafic (în pixeli)
			mouse_pos = GetLocalMousePosition();
			mouse_pos.x = Mathf.Clamp(mouse_pos.x, 50, 1850);
			mouse_pos.y = Mathf.Clamp(mouse_pos.y, 100, 750);

			//Convertirea coordonatei x a mouseului din pixeli în Hz
			frequency = Mathf.RoundToInt(Mathf.Pow(10, (mouse_pos.x+730.618f)/gr_scale_x));
			
			//Selectarea celui mai apropiat punct de mouse (având în vedere doar coordonata x)
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

			//Convertirea coordonatei y a mouseului în dB
			decibels = Mathf.Stepify(170-(mouse_pos.y+400)/gr_scale_y, 0.1f);

			//Schimbarea poziției/Redimensionarea imaginii din spatele graficului
			float x_move = Input.GetActionStrength("ui_right") - Input.GetActionStrength("ui_left");
			float y_move = Input.GetActionStrength("ui_down") - Input.GetActionRawStrength("ui_up");
			
			//La apăsarea butonului CTRL+săgeți se va schimba poziția
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
			//La apăsarea butonului ALT+săgeți se va schimba dimensiunea
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

			//Schimbarea dimensiunii graficului utilizând rotița mouseului
			int zoom = Convert.ToInt16(Input.IsActionJustReleased("wheel_down")) - Convert.ToInt16(Input.IsActionJustReleased("wheel_up"));
			Vector2 add_zoom = Vector2.One*zoom*zoom_spd*Scale;
			Scale -= add_zoom;
			Position += GetLocalMousePosition()*add_zoom;

			//Schimbarea poziției graficului utilizând click dreapta apăsat + mișarea mouseului
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


			//Schimbarea poziției unui punct de pe grafic la apăsarea unui click stânga (punctele pot fi mutate doar pe axa verticală)
			if (Input.IsActionPressed("mb_left"))
			{
				if (mouse_pos == GetLocalMousePosition())
				{
					add_target_value();
				}
			}

			//Încărcarea unui grafic din clipboard la apăsarea CTRL+V
			if (Input.IsActionJustPressed("paste"))
			{
				import_graphiceq_from_clipboard();
			}

			//Exportarea unui grafic în clipboard la apăsarea CTRL+C
			if (Input.IsActionJustPressed("copy"))
			{
				export_graphiceq_to_clipboard();
			}

			//Desenare grafic (funcția _Draw())
			Update();
		}
	}

	public override void _Draw()
	{
		//Desenarea axelor graficului + axe de referință
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

		//Desenarea locației mouseului pe grafic
		if (mouse_pos == GetLocalMousePosition())
		{
			DrawLine(new Vector2(mouse_pos.x, 750), new Vector2(mouse_pos.x, 100), Colors.Yellow);
			DrawCircle(mouse_pos, 3, Colors.Blue);
			String fr = frequency.ToString()+"Hz";
			String db = decibels.ToString()+"dB";
			DrawString(font, new Vector2(mouse_pos.x, 50), fr);
			DrawString(font, new Vector2(mouse_pos.x, 80), db);
		}

		//Desenarea graficelor de comparație
		VBoxContainer list =GetParent().GetNode<VBoxContainer>("UI/Legend/List");
		int nr = list.GetChildCount();
		
		for (int k = 2; k<nr; k++)
		{
			Panel item = list.GetChild<Panel>(k);
			if ((bool)item.Get("visible") == true)
			{
				DrawPolyline((Vector2[])item.Get("data_points"), item.GetNode<ColorRect>("Color").Color, 1.5f, true);
			}
		}

		//Desenarea graficului principal
		DrawPolyline(target, Colors.Yellow, 1.0f, true);

		foreach (Vector2 i in target)
		{
			DrawCircle(i, 1.0f, Colors.Yellow);
		}

	}
}
