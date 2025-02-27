﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Logs;
using SecureStuff;
using Random = UnityEngine.Random;

public class TestVariableViewerScript : MonoBehaviour
{

	public List<SpriteDataSO> Sprites = new List<SpriteDataSO>();


	public SpriteDataSO Sprite;
	public ItemTrait ItemTrait;


	public List<GameObject> PrefabReferences = new List<GameObject>();

	public List<Tool> PrefabComponentReferences = new List<Tool>();

	public List<Tool> ComponentReferences = new List<Tool>();

	public List<GameObject> GameObjectReferences = new List<GameObject>();


	public GameObject PrefabReference;

	public Tool PrefabComponentReference;

	public Tool ComponentReference;

	public GameObject GameObjectReference;


	[VVNote(VVHighlight.SafeToModify)] public bool Pbool = true;

	[VVNote(VVHighlight.UnsafeToModify)] public int Pint = 55;

	[VVNote(VVHighlight.SafeToModify100)]
	public string pstring = "yoyyyoy";

	[VVNote(VVHighlight.VariableChangeUpdate)]
	public Teststruct pTeststruct;

	[VVNote(VVHighlight.DEBUG)] public Connection pConnection = Connection.Overlap;


	public Tuple<int, string> Trees;

	private Connection _state;

	public Connection State
	{
		get { return _state; }
		set
		{
			if (_state != value)
			{
				_state = value;
			}
		}
	}

	public List<int> PListInt = new List<int>();
	public List<bool> PListbool = new List<bool>();
	public List<string> PListstring = new List<string>();
	public List<Teststruct> PListTeststruct = new List<Teststruct>();
	public List<Connection> PListConnection = new List<Connection>();

	public HashSet<int> PHashSetInt = new HashSet<int>();
	public HashSet<bool> PHashSetbool = new HashSet<bool>();
	public HashSet<string> PHashSetstring = new HashSet<string>();
	public HashSet<Connection> PHashSetConnection = new HashSet<Connection>();
	public HashSet<object> PHashSetobject = new HashSet<object>();

	public Dictionary<int, int> PDictionaryIntInt = new Dictionary<int, int>();
	public Dictionary<bool, bool> PDictionaryboolbool = new Dictionary<bool, bool>();
	public Dictionary<string, string> PDictionarystringstring = new Dictionary<string, string>();

	public Dictionary<Connection, Connection>
		PDictionaryConnectionConnection = new Dictionary<Connection, Connection>();

	public Dictionary<string, HashSet<int>> DictionaryHashSet = new Dictionary<string, HashSet<int>>();
	public Dictionary<string, List<int>> DictionaryList = new Dictionary<string, List<int>>();

	public int length = 10;

	public Color Colour = Color.white;

	public List<Color> ColourList = new  List<Color>();
	private void DOThingPrivate()
	{
		Loggy.Log("DOThingPrivate");
	}


	public void DOThingPublic()
	{
		Loggy.Log("DOThingPublic");
	}


	void Start()
	{
		Trees = new Tuple<int, string>(2, "ggggggg");
		for (int i = 0; i < length; i++)
		{
			ColourList.Add(new Color(Random.value, Random.value, Random.value));
			PListInt.Add(i);
			PListbool.Add(true);
			PListstring.Add(i.ToString() + "< t");
			PListConnection.Add(Connection.East);
			var GG = new Teststruct
			{
				author = ("BOB" + i),
				price = i,
				title = i + "COOL?"
			};
			pTeststruct = GG;
			PListTeststruct.Add(GG);
			PHashSetInt.Add(i);
			PHashSetbool.Add(true);
			PHashSetstring.Add(i.ToString() + "< t");
			PHashSetConnection.Add(Connection.East);

			PDictionaryIntInt[i] = i;
			PDictionaryboolbool[true] = true;
			PDictionarystringstring[i.ToString()] = "titymm";
			PDictionaryConnectionConnection[Connection.MachineConnect] = Connection.East;

			DictionaryHashSet[i.ToString()] = PHashSetInt;
			DictionaryList[i.ToString()] = PListInt;
		}
	}
}


public struct Teststruct
{
	public decimal price;
	public string title;
	public string author;
}