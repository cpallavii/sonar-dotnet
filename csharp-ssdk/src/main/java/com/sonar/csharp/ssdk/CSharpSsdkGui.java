/*
 * Copyright (C) 2010 SonarSource SA
 * All rights reserved
 * mailto:contact AT sonarsource DOT com
 */
package com.sonar.csharp.ssdk;

import com.sonar.csharp.squid.parser.CSharpParser;
import com.sonarsource.sdk.SsdkGui;

public final class CSharpSsdkGui {

  private CSharpSsdkGui() {
  }

  public static void main(String[] args) {
    System.setProperty("com.apple.mrj.application.apple.menu.about.name", "SSDK");
    SsdkGui ssdkGui = new SsdkGui(new CSharpParser(), CSharpSourceCodeColorizer.getTokenizers());
    ssdkGui.setVisible(true);
    ssdkGui.setSize(1000, 800);
    ssdkGui.setTitle("C# : SonarSource Development Kit");
  }

}
