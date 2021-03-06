using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using NearFutureElectrical;
using KSP.UI.Screens;

namespace NearFutureElectrical.UI
{
  public class UIResources
  {

    private Dictionary<string, AtlasIcon> iconList;
    private Dictionary<string, GUIStyle> styleList;

    private Texture generalIcons;
    private Texture reactorIcons;

    // Get any icon, given its name
    public AtlasIcon GetIcon(string name)
    {
      return iconList[name];
    }

    // Get a reactor icon, given its ID
    public AtlasIcon GetReactorIcon(int id)
    {
      return iconList[String.Format("reactor_{0}",id)];
    }

    // Get a style, given its name
    public GUIStyle GetStyle(string name)
    {
      return styleList[name];
    }

    // Constructor
    public UIResources()
    {
      CreateIconList();
      CreateStyleList();
    }

    // Iniitializes the icon database
    private void CreateIconList()
    {
      generalIcons = (Texture)GameDatabase.Instance.GetTexture("NearElectrical/UI/icon_general", false);
      reactorIcons = (Texture)GameDatabase.Instance.GetTexture("NearFutureElectrical/UI/icon_reactor", false);

      iconList = new Dictionary<string, AtlasIcon>();

      // Add the general icons
      iconList.Add("lightning", new AtlasIcon(generalIcons, 0.00f, 0.75f, 0.25f, 0.25f));
      iconList.Add("fire", new AtlasIcon(generalIcons, 0.25f, 0.75f, 0.25f, 0.25f));
      iconList.Add("thermometer", new AtlasIcon(generalIcons, 0.50f, 0.75f, 0.25f, 0.25f));
      iconList.Add("timer", new AtlasIcon(generalIcons, 0.75f, 0.75f, 0.25f, 0.25f));

      iconList.Add("notch", new AtlasIcon(generalIcons, 0.0f, 0.50f, 0.25f, 0.25f));
      iconList.Add("gear", new AtlasIcon(generalIcons, 0.25f, 0.50f, 0.25f, 0.25f));
      iconList.Add("capacitor", new AtlasIcon(generalIcons, 0.50f, 0.50f, 0.25f, 0.25f));
      iconList.Add("throttle", new AtlasIcon(generalIcons, 0.75f, 0.50f, 0.25f, 0.25f));

      iconList.Add("throttle_auto", new AtlasIcon(generalIcons, 0.00f, 0.25f, 0.25f, 0.25f));
      iconList.Add("warp_limit", new AtlasIcon(generalIcons, 0.25f, 0.25f, 0.25f, 0.25f));
      iconList.Add("heat_limit", new AtlasIcon(generalIcons, 0.50f, 0.25f, 0.25f, 0.25f));

      // Add the reactor icons
      iconList.Add("reactor_1", new AtlasIcon(reactorIcons, 0.00f, 0.66f, 0.33f, 0.33f));
      iconList.Add("reactor_2", new AtlasIcon(reactorIcons, 0.33f, 0.66f, 0.33f, 0.33f));
      iconList.Add("reactor_3", new AtlasIcon(reactorIcons, 0.66f, 0.66f, 0.33f, 0.33f));
      iconList.Add("reactor_4", new AtlasIcon(reactorIcons, 0.00f, 0.33f, 0.33f, 0.33f));
      iconList.Add("reactor_5", new AtlasIcon(reactorIcons, 0.33f, 0.33f, 0.33f, 0.33f));
      iconList.Add("reactor_6", new AtlasIcon(reactorIcons, 0.66f, 0.33f, 0.33f, 0.33f));
      iconList.Add("reactor_7", new AtlasIcon(reactorIcons, 0.00f, 0.00f, 0.33f, 0.33f));
      iconList.Add("reactor_8", new AtlasIcon(reactorIcons, 0.33f, 0.00f, 0.33f, 0.33f));
      iconList.Add("reactor_9", new AtlasIcon(reactorIcons, 0.66f, 0.00f, 0.33f, 0.33f));

    }

    // Initializes all the styles
    private void CreateStyleList()
    {
        styleList = new Dictionary<string, GUIStyle>();

        GUIStyle draftStyle;

        // Window
        draftStyle = new GUIStyle(HighLogic.Skin.window);
        styleList.Add("window_main", new GUIStyle(draftStyle));
        // Header1
        draftStyle = new GUIStyle(HighLogic.Skin.label);
        draftStyle.fontStyle = FontStyle.Bold;
        draftStyle.alignment = TextAnchor.UpperLeft;
        draftStyle.fontSize = 12;
        draftStyle.stretchWidth = true;
        styleList.Add("header_basic", new GUIStyle(draftStyle));
        // Header 2
        draftStyle.alignment = TextAnchor.MiddleLeft;
        styleList.Add("header_center", new GUIStyle(draftStyle));
        // Basic text
        draftStyle = new GUIStyle(HighLogic.Skin.label);
        draftStyle.fontSize = 11;
        draftStyle.alignment = TextAnchor.MiddleLeft;
        styleList.Add("text_basic", new GUIStyle(draftStyle));
        // Area Background
        draftStyle = new GUIStyle(HighLogic.Skin.textArea);
        draftStyle.active = draftStyle.hover = draftStyle.normal;
        styleList.Add("block_background", new GUIStyle(draftStyle));
        // Toggle
        draftStyle = new GUIStyle(HighLogic.Skin.toggle);
        draftStyle.normal.textColor = draftStyle.normal.textColor;
        styleList.Add("button_toggle", new GUIStyle(draftStyle));
        // Overlaid button
        draftStyle = new GUIStyle(HighLogic.Skin.button);
        draftStyle.normal.textColor = draftStyle.normal.textColor;
        styleList.Add("button_overlaid", new GUIStyle(draftStyle));
        // Progress bar
        // background
        draftStyle = new GUIStyle(HighLogic.Skin.textField);
        draftStyle.active = draftStyle.hover = draftStyle.normal;
        styleList.Add("bar_background", new GUIStyle(draftStyle));
        // foreground
        draftStyle = new GUIStyle(HighLogic.Skin.button);
        draftStyle.active = draftStyle.hover = draftStyle.normal;
        draftStyle.border = GetStyle("bar_background").border;
        draftStyle.padding = GetStyle("bar_background").padding;
        styleList.Add("bar_foreground", new GUIStyle(draftStyle));
    }

  }

  // Represents an atlased icon via a source texture and rectangle
  public class AtlasIcon
  {
    public Texture iconAtlas;
    public Rect iconRect;

    public AtlasIcon(Texture theAtlas, float bl_x, float bl_y, float x_size, float y_size)
    {
      iconAtlas = theAtlas;
      iconRect = new Rect(bl_x, bl_y, x_size, y_size);
    }
  }

}
