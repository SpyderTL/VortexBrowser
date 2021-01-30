using System;
using System.Linq;
using System.Windows.Forms;
using Snes;
using Vortex;

namespace VortexBrowser
{
	internal class ProgramBrowser
	{
		internal static BrowserForm Form;

		internal static void Show()
		{
			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Form = new BrowserForm();
			Form.Text = "Vortex";

			var romNode = Form.TreeView.Nodes.Add("Rom", "Vortex");

			var apuNode = romNode.Nodes.Add("APU", "APU Data");

			var songsNode = romNode.Nodes.Add("Songs", "Songs");

			for (var song = 0; song < VortexRom.SongCount; song++)
			{
				var songNode = songsNode.Nodes.Add(song.ToString(), "Song " + song);
				songNode.Nodes.Add("Loading...");
			}

			Form.TreeView.AfterSelect += TreeView_AfterSelect;
			Form.TreeView.BeforeExpand += TreeView_BeforeExpand;
			Form.ListView.MouseDoubleClick += ListView_MouseDoubleClick;

			RomFile.Load(@"C:\Games\Super Nintendo\Roms\Vortex (U) [!].smc");

			Application.Run(Form);
		}

		private static void ListView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			var item = Form.ListView.SelectedItems.Cast<ListViewItem>().FirstOrDefault();

			if (item == null)
				return;

			var songPosition = int.Parse(item.Text, System.Globalization.NumberStyles.HexNumber);

			SongReader.Position = songPosition;

			SongWindow.Show();
		}

		private static void TreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
		{
			if (e.Node.Parent == null)
				return;

			switch (e.Node.Parent.Name)
			{
				case "Songs":
					e.Node.Nodes.Clear();

					var songBlock = int.Parse(e.Node.Name);

					var address = VortexRom.SongPointerTable + (songBlock * 2);

					Compression.Position = Snes.Snes.Memory[address] | (Snes.Snes.Memory[address + 1] << 8) | 0x0f0000;
					Compression.Decompress();

					Array.Copy(Compression.Data, 0, Snes.Snes.Memory, 0x701000, Compression.Data.Length);

					var position = 0x701000;
					int startAddress;

					while (true)
					{
						var length = BitConverter.ToUInt16(Snes.Snes.Memory, position);
						position += 2;

						if (length == 0)
						{
							startAddress = BitConverter.ToUInt16(Snes.Snes.Memory, position);
							break;
						}

						address = BitConverter.ToUInt16(Snes.Snes.Memory, position);
						position += 2;

						Array.Copy(Snes.Snes.Memory, position, Apu.Memory, address, length);

						position += length;
					}

					position = startAddress;

					for (var song = 0; song < 16; song++)
					{
						address = position;
						var songPosition = Apu.Memory[position++] | (Apu.Memory[position++] << 8);

						var songNode = e.Node.Nodes.Add(address.ToString("X4") + ": Song " + songPosition.ToString("X4"));

						while (true)
						{
							address = songPosition;
							var trackPosition = Apu.Memory[songPosition++] | (Apu.Memory[songPosition++] << 8);

							if ((trackPosition >> 8) == 0x00)
							{
								var repeatPosition = Apu.Memory[songPosition++] | (Apu.Memory[songPosition++] << 8);
								songNode.Nodes.Add(address.ToString("X4") + ": Loop: " + repeatPosition.ToString("X4"));
								break;
							}

							var trackNode = songNode.Nodes.Add(address.ToString("X4") + ": Track " + trackPosition.ToString("X4"));

							for (var channel = 0; channel < 8; channel++)
							{
								address = trackPosition;
								var channelNode = trackNode.Nodes.Add(address.ToString("X4") + ": Channel " + (channel + 1));

								var channelPosition = Apu.Memory[trackPosition++] | (Apu.Memory[trackPosition++] << 8);

								if (channelPosition != 0x0000)
								{
									var readingChannel = true;

									while (readingChannel)
									{
										address = channelPosition;
										int value = Apu.Memory[channelPosition++];

										switch (value)
										{
											case 0x00:
												channelNode.Nodes.Add(address.ToString("X4") + ": 00: End");
												readingChannel = false;
												break;

											case 0xC0:
												value = Apu.Memory[channelPosition++];
												channelNode.Nodes.Add(address.ToString("X4") + ": C0: Unknown: " + value.ToString("X2"));
												break;

											case 0xC8:
												channelNode.Nodes.Add(address.ToString("X4") + ": C8: Tie");
												break;

											case 0xC9:
												channelNode.Nodes.Add(address.ToString("X4") + ": C9: Rest");
												break;

											case 0xCE:
												value = Apu.Memory[channelPosition++];
												channelNode.Nodes.Add(address.ToString("X4") + ": CE: Instrument: " + value.ToString("X2"));
												break;

											case 0xCF:
												value = Apu.Memory[channelPosition++];
												channelNode.Nodes.Add(address.ToString("X4") + ": CF: Unknown: " + value.ToString("X2"));
												break;

											case 0xD0:
												value = Apu.Memory[channelPosition++] | (Apu.Memory[channelPosition++] << 8);
												channelNode.Nodes.Add(address.ToString("X4") + ": D0: Unknown: " + value.ToString("X4"));
												break;

											case 0xD1:
												value = Apu.Memory[channelPosition++] | (Apu.Memory[channelPosition++] << 8) | (Apu.Memory[channelPosition++] << 16);
												channelNode.Nodes.Add(address.ToString("X4") + ": D1: Unknown: " + value.ToString("X6"));
												break;

											case 0xD2:
												channelNode.Nodes.Add(address.ToString("X4") + ": D2: Unknown");
												break;

											case 0xD3:
												value = Apu.Memory[channelPosition++];
												channelNode.Nodes.Add(address.ToString("X4") + ": D3: Unknown: " + value.ToString("X2"));
												break;

											case 0xD4:
												value = Apu.Memory[channelPosition++] | (Apu.Memory[channelPosition++] << 8);
												channelNode.Nodes.Add(address.ToString("X4") + ": D4: Unknown: " + value.ToString("X4"));
												break;

											case 0xD5:
												value = Apu.Memory[channelPosition++];
												channelNode.Nodes.Add(address.ToString("X4") + ": D5: Tempo: " + value.ToString("X2"));
												break;

											case 0xD6:
												value = Apu.Memory[channelPosition++] | (Apu.Memory[channelPosition++] << 8);
												channelNode.Nodes.Add(address.ToString("X4") + ": D6: Unknown: " + value.ToString("X4"));
												break;

											case 0xD7:
												value = Apu.Memory[channelPosition++];
												channelNode.Nodes.Add(address.ToString("X4") + ": D7: Unknown: " + value.ToString("X2"));
												break;

											case 0xD8:
												value = Apu.Memory[channelPosition++];
												channelNode.Nodes.Add(address.ToString("X4") + ": D8: Transpose: " + value.ToString("X2"));
												break;

											case 0xD9:
												value = Apu.Memory[channelPosition++];
												channelNode.Nodes.Add(address.ToString("X4") + ": D9: Unknown: " + value.ToString("X2"));
												break;

											case 0xDA:
												value = Apu.Memory[channelPosition++];
												channelNode.Nodes.Add(address.ToString("X4") + ": DA: Unknown: " + value.ToString("X2"));
												break;

											case 0xDB:
												value = Apu.Memory[channelPosition++];
												channelNode.Nodes.Add(address.ToString("X4") + ": DB: Volume: " + value.ToString("X2"));
												break;

											case 0xDC:
												value = Apu.Memory[channelPosition++] | (Apu.Memory[channelPosition++] << 8);
												channelNode.Nodes.Add(address.ToString("X4") + ": DC: Unknown: " + value.ToString("X4"));
												break;

											case 0xDD:
												var subroutinePosition = Apu.Memory[channelPosition++] | (Apu.Memory[channelPosition++] << 8);
												var repeat = Apu.Memory[channelPosition++];

												var subroutineNode = channelNode.Nodes.Add(address.ToString("X4") + ": DD: Call: " + subroutinePosition.ToString("X4") + " Repeat: " + repeat);

												var readingSubroutine = true;

												while (readingSubroutine)
												{
													address = subroutinePosition;
													value = Apu.Memory[subroutinePosition++];

													switch (value)
													{
														case 0x00:
															subroutineNode.Nodes.Add(address.ToString("X4") + ": 00: End");
															readingSubroutine = false;
															break;

														case 0xC0:
															value = Apu.Memory[channelPosition++];
															channelNode.Nodes.Add(address.ToString("X4") + ": C0: Unknown: " + value.ToString("X2"));
															break;

														case 0xC8:
															subroutineNode.Nodes.Add(address.ToString("X4") + ": C8: Tie");
															break;

														case 0xC9:
															subroutineNode.Nodes.Add(address.ToString("X4") + ": C9: Rest");
															break;

														case 0xCE:
															value = Apu.Memory[subroutinePosition++];
															subroutineNode.Nodes.Add(address.ToString("X4") + ": CE: Instrument: " + value.ToString("X2"));
															break;

														case 0xCF:
															value = Apu.Memory[subroutinePosition++];
															subroutineNode.Nodes.Add(address.ToString("X4") + ": CF: Unknown: " + value.ToString("X2"));
															break;

														case 0xD0:
															value = Apu.Memory[subroutinePosition++] | (Apu.Memory[subroutinePosition++] << 8);
															subroutineNode.Nodes.Add(address.ToString("X4") + ": D0: Unknown: " + value.ToString("X4"));
															break;

														case 0xD1:
															value = Apu.Memory[subroutinePosition++] | (Apu.Memory[subroutinePosition++] << 8) | (Apu.Memory[subroutinePosition++] << 16);
															subroutineNode.Nodes.Add(address.ToString("X4") + ": D1: Unknown: " + value.ToString("X6"));
															break;

														case 0xD2:
															subroutineNode.Nodes.Add(address.ToString("X4") + ": D2: Unknown");
															break;

														case 0xD3:
															value = Apu.Memory[subroutinePosition++];
															subroutineNode.Nodes.Add(address.ToString("X4") + ": D3: Unknown: " + value.ToString("X2"));
															break;

														case 0xD4:
															value = Apu.Memory[subroutinePosition++] | (Apu.Memory[subroutinePosition++] << 8);
															subroutineNode.Nodes.Add(address.ToString("X4") + ": D4: Unknown: " + value.ToString("X4"));
															break;

														case 0xD5:
															value = Apu.Memory[subroutinePosition++];
															subroutineNode.Nodes.Add(address.ToString("X4") + ": D5: Tempo: " + value.ToString("X2"));
															break;

														case 0xD6:
															value = Apu.Memory[subroutinePosition++] | (Apu.Memory[subroutinePosition++] << 8);
															subroutineNode.Nodes.Add(address.ToString("X4") + ": D6: Unknown: " + value.ToString("X4"));
															break;

														case 0xD7:
															value = Apu.Memory[subroutinePosition++];
															subroutineNode.Nodes.Add(address.ToString("X4") + ": D7: Unknown: " + value.ToString("X2"));
															break;

														case 0xD8:
															value = Apu.Memory[subroutinePosition++];
															subroutineNode.Nodes.Add(address.ToString("X4") + ": D8: Transpose: " + value.ToString("X2"));
															break;

														case 0xD9:
															value = Apu.Memory[subroutinePosition++];
															subroutineNode.Nodes.Add(address.ToString("X4") + ": D9: Unknown: " + value.ToString("X2"));
															break;

														case 0xDA:
															value = Apu.Memory[subroutinePosition++];
															subroutineNode.Nodes.Add(address.ToString("X4") + ": DA: Unknown: " + value.ToString("X2"));
															break;

														case 0xDB:
															value = Apu.Memory[subroutinePosition++];
															subroutineNode.Nodes.Add(address.ToString("X4") + ": DB: Volume: " + value.ToString("X2"));
															break;

														case 0xDC:
															value = Apu.Memory[subroutinePosition++] | (Apu.Memory[subroutinePosition++] << 8);
															subroutineNode.Nodes.Add(address.ToString("X4") + ": DC: Unknown: " + value.ToString("X4"));
															break;

														case 0xDD:
															var subroutinePosition2 = Apu.Memory[subroutinePosition++] | (Apu.Memory[subroutinePosition++] << 8);
															var repeat2 = Apu.Memory[subroutinePosition++];

															subroutineNode.Nodes.Add(address.ToString("X4") + ": DD: Call: " + subroutinePosition2.ToString("X4") + " Repeat: " + repeat2);
															break;

														case 0xDE:
															value = Apu.Memory[subroutinePosition++];
															subroutineNode.Nodes.Add(address.ToString("X4") + ": DE: Unknown: " + value.ToString("X2"));
															break;

														case 0xDF:
															value = Apu.Memory[subroutinePosition++] | (Apu.Memory[subroutinePosition++] << 8) | (Apu.Memory[subroutinePosition++] << 16);
															subroutineNode.Nodes.Add(address.ToString("X4") + ": DF: Pitch Bend: " + value.ToString("X6"));
															break;

														case 0xE0:
															value = Apu.Memory[subroutinePosition++] | (Apu.Memory[subroutinePosition++] << 8) | (Apu.Memory[subroutinePosition++] << 16);
															subroutineNode.Nodes.Add(address.ToString("X4") + ": E0: Unknown: " + value.ToString("X6"));
															break;

														case 0xE1:
															subroutineNode.Nodes.Add(address.ToString("X4") + ": E1: Unknown");
															break;

														case 0xE2:
															value = Apu.Memory[subroutinePosition++];
															subroutineNode.Nodes.Add(address.ToString("X4") + ": E2: Unknown: " + value.ToString("X2"));
															break;

														case 0xE3:
															value = Apu.Memory[subroutinePosition++] | (Apu.Memory[subroutinePosition++] << 8) | (Apu.Memory[subroutinePosition++] << 16);
															subroutineNode.Nodes.Add(address.ToString("X4") + ": E3: Unknown: " + value.ToString("X6"));
															break;

														case 0xE4:
															subroutineNode.Nodes.Add(address.ToString("X4") + ": E4: Unknown");
															break;

														case 0xE5:
															value = Apu.Memory[subroutinePosition++] | (Apu.Memory[subroutinePosition++] << 8) | (Apu.Memory[subroutinePosition++] << 16);
															subroutineNode.Nodes.Add(address.ToString("X4") + ": E5: Unknown: " + value.ToString("X6"));
															break;

														case 0xE6:
															value = Apu.Memory[subroutinePosition++] | (Apu.Memory[subroutinePosition++] << 8) | (Apu.Memory[subroutinePosition++] << 16);
															subroutineNode.Nodes.Add(address.ToString("X4") + ": E6: Unknown: " + value.ToString("X6"));
															break;

														case 0xE7:
															value = Apu.Memory[subroutinePosition++] | (Apu.Memory[subroutinePosition++] << 8) | (Apu.Memory[subroutinePosition++] << 16);
															subroutineNode.Nodes.Add(address.ToString("X4") + ": E7: Unknown: " + value.ToString("X6"));
															break;

														case 0xE8:
															value = Apu.Memory[subroutinePosition++];
															subroutineNode.Nodes.Add(address.ToString("X4") + ": E8: Unknown: " + value.ToString("X2"));
															break;

														case 0xE9:
															value = Apu.Memory[subroutinePosition++];
															subroutineNode.Nodes.Add(address.ToString("X4") + ": E9: Unknown: " + value.ToString("X2"));
															break;

														case 0xEA:
															subroutineNode.Nodes.Add(address.ToString("X4") + ": EA: Unknown");
															break;

														case 0xEB:
															subroutineNode.Nodes.Add(address.ToString("X4") + ": EB: Unknown");
															break;

														case 0xEC:
															subroutineNode.Nodes.Add(address.ToString("X4") + ": EC: Unknown");
															break;

														case 0xED:
															value = Apu.Memory[subroutinePosition++];
															subroutineNode.Nodes.Add(address.ToString("X4") + ": ED: Unknown: " + value.ToString("X2"));
															break;

														case 0xEE:
															value = Apu.Memory[subroutinePosition++];
															subroutineNode.Nodes.Add(address.ToString("X4") + ": EE: Unknown: " + value.ToString("X2"));
															break;

														case 0xEF:
															value = Apu.Memory[subroutinePosition++];
															subroutineNode.Nodes.Add(address.ToString("X4") + ": EF: Unknown: " + value.ToString("X2"));
															break;

														case 0xF0:
															subroutineNode.Nodes.Add(address.ToString("X4") + ": F0: Unknown");
															break;

														default:
															if ((value & 0x80) == 0x00)
															{
																// Parameters
																subroutineNode.Nodes.Add(address.ToString("X4") + ": " + value.ToString("X2") + ": Parameters");
															}
															else if (value < 0xC0)
															{
																// Note
																subroutineNode.Nodes.Add(address.ToString("X4") + ": " + value.ToString("X2") + ": Note: " + (value - 0x80).ToString("X2"));
															}
															else
																subroutineNode.Nodes.Add(address.ToString("X4") + ": " + value.ToString("X2"));
															break;
													}
												}
												break;

											case 0xDE:
												value = Apu.Memory[channelPosition++];
												channelNode.Nodes.Add(address.ToString("X4") + ": DE: Unknown: " + value.ToString("X2"));
												break;

											case 0xDF:
												value = Apu.Memory[channelPosition++] | (Apu.Memory[channelPosition++] << 8) | (Apu.Memory[channelPosition++] << 16);
												channelNode.Nodes.Add(address.ToString("X4") + ": DF: Pitch Bend: " + value.ToString("X6"));
												break;

											case 0xE0:
												value = Apu.Memory[channelPosition++] | (Apu.Memory[channelPosition++] << 8) | (Apu.Memory[channelPosition++] << 16);
												channelNode.Nodes.Add(address.ToString("X4") + ": E0: Unknown: " + value.ToString("X6"));
												break;

											case 0xE1:
												channelNode.Nodes.Add(address.ToString("X4") + ": E1: Unknown");
												break;

											case 0xE2:
												value = Apu.Memory[channelPosition++];
												channelNode.Nodes.Add(address.ToString("X4") + ": E2: Unknown: " + value.ToString("X2"));
												break;

											case 0xE3:
												value = Apu.Memory[channelPosition++] | (Apu.Memory[channelPosition++] << 8) | (Apu.Memory[channelPosition++] << 16);
												channelNode.Nodes.Add(address.ToString("X4") + ": E3: Unknown: " + value.ToString("X6"));
												break;

											case 0xE4:
												channelNode.Nodes.Add(address.ToString("X4") + ": E4: Unknown");
												break;

											case 0xE5:
												value = Apu.Memory[channelPosition++] | (Apu.Memory[channelPosition++] << 8) | (Apu.Memory[channelPosition++] << 16);
												channelNode.Nodes.Add(address.ToString("X4") + ": E5: Unknown: " + value.ToString("X6"));
												break;

											case 0xE6:
												value = Apu.Memory[channelPosition++] | (Apu.Memory[channelPosition++] << 8) | (Apu.Memory[channelPosition++] << 16);
												channelNode.Nodes.Add(address.ToString("X4") + ": E6: Unknown: " + value.ToString("X6"));
												break;

											case 0xE7:
												value = Apu.Memory[channelPosition++] | (Apu.Memory[channelPosition++] << 8) | (Apu.Memory[channelPosition++] << 16);
												channelNode.Nodes.Add(address.ToString("X4") + ": E7: Unknown: " + value.ToString("X6"));
												break;

											case 0xE8:
												value = Apu.Memory[channelPosition++];
												channelNode.Nodes.Add(address.ToString("X4") + ": E8: Unknown: " + value.ToString("X2"));
												break;

											case 0xE9:
												value = Apu.Memory[channelPosition++];
												channelNode.Nodes.Add(address.ToString("X4") + ": E9: Unknown: " + value.ToString("X2"));
												break;

											case 0xEA:
												channelNode.Nodes.Add(address.ToString("X4") + ": EA: Unknown");
												break;

											case 0xEB:
												channelNode.Nodes.Add(address.ToString("X4") + ": EB: Unknown");
												break;

											case 0xEC:
												channelNode.Nodes.Add(address.ToString("X4") + ": EC: Unknown");
												break;

											case 0xED:
												value = Apu.Memory[channelPosition++];
												channelNode.Nodes.Add(address.ToString("X4") + ": ED: Unknown: " + value.ToString("X2"));
												break;

											case 0xEE:
												value = Apu.Memory[channelPosition++];
												channelNode.Nodes.Add(address.ToString("X4") + ": EE: Unknown: " + value.ToString("X2"));
												break;

											case 0xEF:
												value = Apu.Memory[channelPosition++];
												channelNode.Nodes.Add(address.ToString("X4") + ": EF: Unknown: " + value.ToString("X2"));
												break;

											case 0xF0:
												channelNode.Nodes.Add(address.ToString("X4") + ": F0: Unknown");
												break;

											default:
												if ((value & 0x80) == 0x00)
												{
													// Parameters
													channelNode.Nodes.Add(address.ToString("X4") + ": " + value.ToString("X2") + ": Parameters");
												}
												else if (value < 0xC0)
												{
													// Note
													channelNode.Nodes.Add(address.ToString("X4") + ": " + value.ToString("X2") + ": Note: " + (value - 0x80).ToString("X2"));
												}
												else
													channelNode.Nodes.Add(address.ToString("X4") + ": " + value.ToString("X2"));
												break;
										}
									}
								}
							}
						}
					}

					break;
			}
		}

		private static void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			Form.ListView.Clear();

			if (e.Node.Parent == null)
				return;

			switch (e.Node.Parent.Name)
			{
				case "Rom":
					switch (e.Node.Name)
					{
						case "APU":
							APU(VortexRom.ApuDataAddress);
							break;
					}
					break;

				case "Songs":
					var song = int.Parse(e.Node.Name);

					var address = VortexRom.SongPointerTable + (song * 2);

					Compression.Position = Snes.Snes.Memory[address] | (Snes.Snes.Memory[address + 1] << 8) | 0x0f0000;
					Compression.Decompress();

					Array.Copy(Compression.Data, 0, Snes.Snes.Memory, 0x701000, Compression.Data.Length);

					Song();
					break;
			}
		}

		private static void Song()
		{
			// Load Song
			Form.ListView.Items.Clear();

			Form.ListView.View = View.Details;
			Form.ListView.Columns.Clear();
			Form.ListView.Columns.Add("Address");

			var position = 0x701000;
			int startAddress;

			while (true)
			{
				var length = BitConverter.ToUInt16(Snes.Snes.Memory, position);
				position += 2;

				if (length == 0)
				{
					startAddress = BitConverter.ToUInt16(Snes.Snes.Memory, position);
					break;
				}

				var address = BitConverter.ToUInt16(Snes.Snes.Memory, position);
				position += 2;

				Array.Copy(Snes.Snes.Memory, position, Apu.Memory, address, length);

				position += length;
			}

			position = startAddress;

			for (var song = 0; song < 16; song++)
			{
				var songPosition = Apu.Memory[position++] | (Apu.Memory[position++] << 8);

				var item = Form.ListView.Items.Add(songPosition.ToString("X4"));
				item.Name = song.ToString();
			}
		}

		private static void SongBlock()
		{
			// Load Song Blocks
			Form.ListView.Items.Clear();

			Form.ListView.View = View.Details;
			Form.ListView.Columns.Clear();
			Form.ListView.Columns.Add("Address");
			Form.ListView.Columns.Add("Destination");
			Form.ListView.Columns.Add("Length");

			var position = 0x701000;

			while (true)
			{
				var length = BitConverter.ToUInt16(Snes.Snes.Memory, position);
				position += 2;

				if (length == 0)
				{
					//var startAddress = BitConverter.ToUInt16(Snes.Memory, position);
					break;
				}

				var address = BitConverter.ToUInt16(Snes.Snes.Memory, position);
				position += 2;

				var item = Form.ListView.Items.Add(position.ToString("X6"));
				item.SubItems.Add(address.ToString("X4"));
				item.SubItems.Add(length.ToString("X4"));

				position += length;
			}
		}

		private static void APU(int position)
		{
			Form.ListView.View = View.Details;
			Form.ListView.Columns.Clear();
			Form.ListView.Columns.Add("Address");
			Form.ListView.Columns.Add("Destination");
			Form.ListView.Columns.Add("Length");

			while (true)
			{
				var length = BitConverter.ToUInt16(Snes.Snes.Memory, position);
				position += 2;

				if (length == 0)
				{
					//var startAddress = BitConverter.ToUInt16(Snes.Memory, position);
					break;
				}

				var address = BitConverter.ToUInt16(Snes.Snes.Memory, position);
				position += 2;

				var item = Form.ListView.Items.Add(position.ToString("X6"));
				item.SubItems.Add(address.ToString("X4"));
				item.SubItems.Add(length.ToString("X4"));

				position += length;
			}
		}
	}
}