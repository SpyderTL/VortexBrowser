using System;
using System.Diagnostics;
using System.Linq;
using Vortex;

namespace Midi
{
	public static class MidiPlayer
	{
		public static int Song;

		public static int MasterVolume;
		public static int MasterTuning;

		public static int[] Notes;
		public static int[] Volume;
		public static int[] Pan;
		public static int[] Instruments;
		public static int[] Drums;
		public static int[] NoteOffsets;
		public static int[] Transpose;
		public static int[] Tuning;
		public static int[] Portamento;

		public static void Start()
		{
			MasterVolume = 0xff;
			Notes = new int[8];
			Pan = Enumerable.Repeat(10, 8).ToArray();
			Volume = Enumerable.Repeat(0xFF, 8).ToArray();
			Instruments = Enumerable.Repeat(0xFF, 8).ToArray();
			Drums = new int[8];
			NoteOffsets = new int[8];
			Transpose = new int[8];
			Tuning = new int[8];
			Portamento = new int[8];

			Midi.Enable();

			for (var channel = 0; channel < 8; channel++)
			{
				Midi.ControlChange(channel, 123, 0);
				Midi.ProgramChange(channel, Instruments[channel]);
				Midi.ControlChange(channel, Midi.Controls.Reverb, 127);
				//Midi.ControlChange(channel, Midi.Controls.Tremolo, 127);
				Midi.ControlChange(channel, Midi.Controls.Chorus, 127);
				//Midi.ControlChange(channel, Midi.Controls.Detune, 127);
				//Midi.ControlChange(channel, Midi.Controls.Phaser, 127);

				Midi.ControlChange(channel, Midi.Controls.Portamento, Portamento[channel]);
			}
		}

		public static void Stop()
		{
			for (var channel = 0; channel < 8; channel++)
				Midi.ControlChange(channel, 123, 0);

			Midi.Disable();
		}

		public static void Update()
		{
			UpdateVolume();

			for (var channel = 0; channel < 8; channel++)
			{
				UpdateTranspose(channel);
				UpdateInstruments(channel);
				UpdateVolume(channel);
				UpdatePan(channel);
				UpdateTuning(channel);
				UpdatePortamento(channel);
				UpdateNotes(channel);
			}
		}

		private static void UpdatePortamento(int channel)
		{
			if (Portamento[channel] != SongPlayer.ChannelPortamento[channel])
			{
				Midi.ControlChange(channel, Midi.Controls.PortamentoEnable, SongPlayer.ChannelPortamento[channel] == 0 ? 0 : 127);
				Midi.ControlChange(channel, Midi.Controls.Portamento, SongPlayer.ChannelPortamento[channel]);
				Portamento[channel] = SongPlayer.ChannelPortamento[channel];
			}
		}

		private static void UpdateTranspose(int channel)
		{
			if (Transpose[channel] != SongPlayer.ChannelTranspose[channel])
			{
				if (Notes[channel] != 0)
				{
					if (Drums[channel] == 0)
					{
						Midi.NoteOff(channel, Notes[channel] + NoteOffsets[channel] + Transpose[channel], 0);

						Midi.NoteOn(channel, Notes[channel] + NoteOffsets[channel] + SongPlayer.ChannelTranspose[channel], (int)((SongPlayer.ChannelVelocities[channel] / 15.0f) * 127.0f));
					}
				}

				Transpose[channel] = SongPlayer.ChannelTranspose[channel];
			}
		}

		private static void UpdateTuning(int channel)
		{
			if (Tuning[channel] != SongPlayer.ChannelTuning[channel])
			{
				int value = 0x2000 + (int)((SongPlayer.ChannelTuning[channel] / 255.0f) * 0x1000);
				Midi.PitchBendChange(channel, value);
				Tuning[channel] = SongPlayer.ChannelTuning[channel];
			}
		}

		private static void UpdateNotes(int channel)
		{
			if (SongPlayer.ChannelNotes[channel] == 0 && Notes[channel] != 0)
			{
				if (Drums[channel] == 0)
					Midi.NoteOff(channel, Notes[channel] + NoteOffsets[channel] + Transpose[channel], 0);
				else
					Midi.NoteOff(9, Drums[channel], 0);

				Notes[channel] = SongPlayer.ChannelNotes[channel];
			}
			else if (SongPlayer.ChannelNoteStart[channel])
			{
				if (Drums[channel] == 0)
				{
					if (Notes[channel] != 0)
						Midi.NoteOff(channel, Notes[channel] + NoteOffsets[channel] + Transpose[channel], 0);

					if (SongPlayer.ChannelNotes[channel] != 0)
						Midi.NoteOn(channel, SongPlayer.ChannelNotes[channel] + NoteOffsets[channel] + Transpose[channel], (int)((SongPlayer.ChannelVelocities[channel] / 15.0f) * 127.0f));
						//Midi.NoteOn(channel, SongPlayer.ChannelNotes[channel] + NoteOffsets[channel] + Transpose[channel], 32);
				}
				else
				{
					if (Notes[channel] != 0)
						Midi.NoteOff(9, Drums[channel], 0);

					if (SongPlayer.ChannelNotes[channel] != 0)
						Midi.NoteOn(9, Drums[channel], (int)((SongPlayer.ChannelVelocities[channel] / 15.0f) * 127.0f));
					//Midi.NoteOn(9, Drums[channel], 127);

					//Midi.NoteOff(9, Drums[channel], 0);
				}

				Notes[channel] = SongPlayer.ChannelNotes[channel];
			}
		}

		private static void UpdatePan(int channel)
		{
			if (Pan[channel] != SongPlayer.ChannelPan[channel])
			{
				var value = (int)((SongPlayer.ChannelPan[channel] / 20.0) * 127.0);

				Midi.ControlChange(channel, Midi.Controls.Pan, value);
				Pan[channel] = SongPlayer.ChannelPan[channel];
			}
		}

		private static void UpdateVolume()
		{
			if (MasterVolume != SongPlayer.Volume)
			{
				for (var channel = 0; channel < 8; channel++)
				{
					var value = (int)((Volume[channel] / (double)0xff) * (SongPlayer.Volume / (double)0xff) * 0x7f);

					Midi.ControlChange(channel, 7, value);
				}

				MasterVolume = SongPlayer.Volume;
			}
		}

		private static void UpdateVolume(int channel)
		{
			if (Volume[channel] != SongPlayer.ChannelVolume[channel])
			{
				var value = (int)((SongPlayer.ChannelVolume[channel] / (double)0xff) * (SongPlayer.Volume / (double)0xff) * 0x7f);

				Midi.ControlChange(channel, 7, value);
				Volume[channel] = SongPlayer.ChannelVolume[channel];
			}
		}

		private static void UpdateInstruments(int channel)
		{
			if (Instruments[channel] != SongPlayer.ChannelInstruments[channel])
			{
				if (Notes[channel] != 0)
				{
					if (Drums[channel] != 0)
						Midi.NoteOff(9, Drums[channel], 0);
					else
						Midi.NoteOff(channel, Notes[channel] + NoteOffsets[channel] + Transpose[channel], 0);

					Notes[channel] = 0;
				}

				var instrument = -1;
				var offset = 0;
				Drums[channel] = 0;

				switch (SongPlayer.ChannelInstruments[channel])
				{
					case 0x00:
						Drums[channel] = Midi.Drums.BassDrum;
						break;

					case 0x01:
						Drums[channel] = Midi.Drums.SnareDrum;
						break;

					case 0x02:
						Drums[channel] = Midi.Drums.HiHat;
						break;

					case 0x13:
						Drums[channel] = Midi.Drums.HandClap;
						break;

					case 0x03:
						instrument = Midi.Patches.PickBass;
						offset = 12;
						break;

					case 0x04:
						//instrument = 50;
						//instrument = 81;
						instrument = 86;
						offset = 36;
						break;

					case 0x05:
						instrument = 48;
						offset = 36;
						break;

					case 0x06:
						instrument = 80;
						offset = 36;
						break;

					case 0x07:
						//instrument = 30;
						instrument = 29;
						//instrument = 86;
						offset = 9;
						break;

					case 0x08:
						instrument = 0;
						offset = 24;
						break;

					case 0x09:
						instrument = 80;
						offset = 36;
						break;

					case 0x0A:
						instrument = 83;
						offset = 24;
						break;

					case 0x0B:
						instrument = 127;
						offset = 0;
						break;

					case 0x0E:
						//instrument = 87;
						instrument = 84;
						offset = 12;
						break;

					default:
						Debug.WriteLine("Unknown Instrument: " + SongPlayer.ChannelInstruments[channel].ToString("X2"));
						instrument = 80;
						offset = 12;
						break;
				}

				if (instrument != -1)
					Midi.ProgramChange(channel, instrument);

				Instruments[channel] = SongPlayer.ChannelInstruments[channel];
				NoteOffsets[channel] = offset;
			}
		}
	}
}