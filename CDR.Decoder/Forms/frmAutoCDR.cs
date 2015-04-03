using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CDR.Decoder;

namespace Decoder.Forms
{
	public partial class frmAutoCDR : Form
	{
		public frmAutoCDR ()
		{
			InitializeComponent ();
		}

		private void button1_Click (object sender, EventArgs e)
		{
			string str;
			str = @"/home/chandanpasunoori/Desktop/DAP_Data/BSNL_Mediation_Data/dm1billing3/FTP_ChandgMSS3/tertiary/tertiary/";
			DoJob_ZTESGSN ("", str, str);
		}

		private BackgroundWorker _worker;
		private IDecoderJob _job;
		private JobStatus _status;
		private BasicLogger _logger;
		private bool _ready;

		private void DoJob_ZTESGSN (string schemaName, string SourceFile, string Destinationfile)
		{

			//for ZTE SGSN CDR decoding process

			CdrDecoder decoder = new CdrDecoder ();
			_job = new JobBase ();
			_job.SourcePath = SourceFile;
			decoder.ElementDefinitionProvider.CurrentSchema = "S-CDR";// "S-SMT-CDR"; "S-SMO-CDR"; //"S-CDR";// // ;//;//  //// schemaName;// _job.DefinitionSchemaName;
			CdrElement record;

			RecordFormatter formatter = (_job.IsFormatterActive && (_job.FormatterSettings != null)) ? new RecordFormatter (_job.FormatterSettings) : null;

			if (_job.IsFilterActive && !String.IsNullOrEmpty (_job.FilterText)) {
				try {
					Regex filterRegex = new Regex (_job.FilterText, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
				} catch (Exception error) {
					return;
				}
			}

			StreamWriter dstFile;

			FileInfo[] cdrFiles = new DirectoryInfo (Path.GetDirectoryName (_job.SourcePath)).GetFiles ("*");
			FileStream cdr;
			long cdrLength;
			long rem;
			string recText;
			string myHeader = "";
			bool headers = true;
			// SGSN patch ///////////////////////////////////////////////////////////
			int trial = 0;
			bool sgsn = !String.IsNullOrEmpty (decoder.ElementDefinitionProvider.Type) && (String.Compare (decoder.ElementDefinitionProvider.Type, "SGSN", true) == 0);
			List<CdrElement> sgsnRecord = new List<CdrElement> ();

			/////////////////////////////////////////////////////////////////////////
			_status = new JobStatus ();
			_status.CdrFilesIn = cdrFiles.Length;
			foreach (FileInfo fi in cdrFiles) {
				dstFile = new StreamWriter (fi.FullName + ".dmp");

				cdr = new FileStream (fi.FullName, FileMode.Open);

				record = decoder.DecodeRecord (cdr, false);

				cdrLength = cdr.Length;

				if (_job.StartOffset > 0)
					cdr.Seek (_job.StartOffset, SeekOrigin.Begin);
				trial = 0;
				_status.RecordsOut = 0;
				_status.CurrentCdrFile = fi.Name;
				myHeader = string.Empty;
				headers = true;
				rem = 0;
				sgsn = true;

				for (;;) {
					if (_status.RecordsOut == 0) {
						if (sgsn && (sgsnRecord.Count >= 0)) {					
							record = decoder.DecodeRecord (cdr, false);
						} else {
							record = decoder.DecodeRecord (cdr, true);
						}
					} else {
						record = decoder.DecodeRecord (cdr, false);
					}
					Console.WriteLine(record);
					if (record == null) {
						trial = trial + 1;
						if (sgsn && (sgsnRecord.Count > 0)) {
							_status.RecordsOut++;
							_status.RecordsOutTotal++;
							_status.Percent = (int)Math.Ceiling ((double)cdr.Position / cdrLength * 100);
							recText = "";
							if (formatter == null) {
								string[] str;
								// StringBuilder sgsnText = new StringBuilder(String.Format("{0,8} > {1} {2}=[", sgsnRecord[0].Offset, _status.RecordsOut, sgsnRecord[0].Name), sgsnRecord.Count + 1);
								//if (sgsnRecord.Count == 1) 
								//    continue;
								try {
									StringBuilder sgsnText = new StringBuilder (String.Format ("{0}", sgsnRecord [1].Name));
									for (int s = 0; s < sgsnRecord.Count; s++) {

										string[] str1;
										string[] separators = { "=" };
										string[] separators2 = { " " };

										str1 = sgsnRecord [s].ToString ().Split (separators2, StringSplitOptions.None);

										str1 = sgsnRecord [s].ToString ().Split (separators2, StringSplitOptions.None);

										recText = null;
										if (str1.Length >= 5) {
											recText = sgsnRecord [s].ToString ();
											try {

                                                 
												recText = recText.Replace ("UMTSGSMPLMNCallDataRecord=", "{\"type\":\"");
												string[] separators3 = { "=" };
												str1 = recText.Split (separators2, StringSplitOptions.None);
												recText = recText.Replace ("=[", ",\",");//:[
												recText = recText.Replace (" ", ",\"");
												recText = recText.Replace ("=", "\":");
												recText = recText.Replace ("=[", ",");//:[
												recText = recText.Replace ("[", "");
												recText = recText.Replace (",\",", "\",\"");
												recText = recText + "}";
												recText = recText.Replace ("]]", "");
											} catch (Exception ex) {


											}
											dstFile.WriteLine (recText);
										}
										if (recText != null) {
											if (headers == true)
                                                //dstFile.WriteLine(myHeader);
                                                headers = false;
											if (str1.Length >= 12)
                                                //dstFile.WriteLine(recText);



                                                recText = null;
										}
									}
								} catch (Exception ex) {
								}

							}

							sgsnRecord.Clear ();

							recText = "";


						}
						if (trial <= 50) {
							continue;
						} else {
							break;
						}
					}

					if (sgsn) {

						if (record.IsConstructed && (record.Path.Equals ("20") || record.Path.Equals ("23") || record.Path.Equals ("24"))) {
							if (sgsnRecord.Count == 0) {
								sgsnRecord.Add (record);
								continue;
							}
						} else {
							sgsnRecord.Add (record);
							continue;
						}
					}



				}

				cdr.Close ();
				dstFile.Close ();
				headers = true;
			}



			_status.ResultCode = JobResultCode.AllOK;

		}

		private void DoJob (string schemaName, string SourceFile, string Destinationfile)
		{

			//for Ericsson SGSN CDR decoding process

			CdrDecoder decoder = new CdrDecoder ();
			_job = new JobBase ();
			_job.SourcePath = SourceFile;
			decoder.ElementDefinitionProvider.CurrentSchema = "S-CDR";// schemaName;// _job.DefinitionSchemaName;
			CdrElement record;

			RecordFormatter formatter = (_job.IsFormatterActive && (_job.FormatterSettings != null)) ? new RecordFormatter (_job.FormatterSettings) : null;
			Regex filterRegex = null;
			if (_job.IsFilterActive && !String.IsNullOrEmpty (_job.FilterText)) {
				try {
					filterRegex = new Regex (_job.FilterText, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
				} catch (Exception error) {
					return;
				}
			}

			StreamWriter dstFile = new StreamWriter (_job.DestinationPath);
			if ((formatter != null) && _job.FormatterSettings.PrintColumnsHeader) {
				dstFile.WriteLine (_job.FormatterSettings.ColumnsHeader);
			}

			FileInfo[] cdrFiles;

			cdrFiles = new DirectoryInfo (Path.GetDirectoryName (_job.SourcePath)).GetFiles (Path.GetFileName (_job.SourcePath), SearchOption.TopDirectoryOnly);
			FileStream cdr;
			long cdrLength;
			long rem;
			string recText;

			// SGSN patch ///////////////////////////////////////////////////////////

			bool sgsn = !String.IsNullOrEmpty (decoder.ElementDefinitionProvider.Type) && (String.Compare (decoder.ElementDefinitionProvider.Type, "SGSN", true) == 0);
			List<CdrElement> sgsnRecord = new List<CdrElement> ();

			/////////////////////////////////////////////////////////////////////////
			_status = new JobStatus ();
			_status.CdrFilesIn = cdrFiles.Length;
			foreach (FileInfo fi in cdrFiles) {
				cdr = new FileStream (fi.FullName, FileMode.Open);
				cdrLength = cdr.Length;

				if (_job.StartOffset > 0)
					cdr.Seek (_job.StartOffset, SeekOrigin.Begin);

				_status.RecordsOut = 0;
				_status.CurrentCdrFile = fi.Name;
				rem = 0;

				if (sgsn) {
					sgsnRecord.Clear ();
				}


				//  _worker.ReportProgress(_status.Percent);
				for (; ;) {
					if (_status.RecordsOut == 0) {
						if (sgsn && (sgsnRecord.Count > 0)) {
							record = decoder.DecodeRecord (cdr, false);
						} else {
							record = decoder.DecodeRecord (cdr, true);
						}
					} else {
						record = decoder.DecodeRecord (cdr, false);
					}
					if (record == null) {
						if (sgsn && (sgsnRecord.Count > 0)) {
							_status.RecordsOut++;
							_status.RecordsOutTotal++;
							_status.Percent = (int)Math.Ceiling ((double)cdr.Position / cdrLength * 100);

							if (formatter == null) {
								string[] str;
								// StringBuilder sgsnText = new StringBuilder(String.Format("{0,8} > {1} {2}=[", sgsnRecord[0].Offset, _status.RecordsOut, sgsnRecord[0].Name), sgsnRecord.Count + 1);
								StringBuilder sgsnText = new StringBuilder (String.Format ("{0}", sgsnRecord [0].Name));
								for (int s = 1; s < sgsnRecord.Count; s++) {
									if (s > 1)
										sgsnText.Append (' ');
									string[] separators = { "=" };
									str = sgsnRecord [s].ToString ().Split (separators, StringSplitOptions.None);
									if (str.Length >= 3) {
										sgsnText.Append (str [1] + str [2]);

									} else {
										sgsnText.Append (str [1]);

									}
									// sgsnText.Append(sgsnRecord[s].ToString());
								}
								sgsnText = sgsnText.Replace ("sgsnPDPRecord", "");
								//sgsnText.Append("]");
								recText = sgsnText.ToString ();
							} else {
								recText = formatter.FormatSGSNRecord (sgsnRecord);
							}

							if ((filterRegex == null) || (filterRegex.Match (recText).Success))
								dstFile.WriteLine (recText);

							Math.DivRem (_status.RecordsOut, 1000, out rem);
							if (rem == 0)
								_worker.ReportProgress (_status.Percent);

						}
						break;
					}

					if (sgsn) {
						//if (_worker.CancellationPending)
						//{
						//    break;
						//}
						if (record.IsConstructed && (record.Path.Equals ("20") || record.Path.Equals ("23") || record.Path.Equals ("24"))) {
							if (sgsnRecord.Count == 0) {
								sgsnRecord.Add (record);
								continue;
							}
						} else {
							sgsnRecord.Add (record);
							continue;
						}
					}

					_status.RecordsOut++;
					_status.RecordsOutTotal++;
					_status.Percent = (int)Math.Ceiling ((double)cdr.Position / cdrLength * 100);
					StringBuilder Heading = new StringBuilder ();
					if (sgsn) {
						if (formatter == null) {
							StringBuilder sgsnText = new StringBuilder (String.Format ("{0}", sgsnRecord [0].Name));
							//sgsnText = "";
							//StringBuilder sgsnText = new StringBuilder(String.Format("{0,8} > {1} {2}=[", sgsnRecord[0].Offset, _status.RecordsOut, sgsnRecord[0].Name), sgsnRecord.Count + 1);
							for (int s = 1; s < sgsnRecord.Count; s++) {
								if (s > 1)
									sgsnText.Append (' ');
								Heading.Append (' ');
								string[] str;
								string[] separators = { "=" };
								str = sgsnRecord [s].ToString ().Split (separators, StringSplitOptions.None);

								//sgsnText.Append(sgsnRecord[s].ToString());
								if (str.Length >= 3) {
									sgsnText.Append (str [1] + str [2]);
									Heading.Append (str [0]);
								} else {
									sgsnText.Append (str [1]);
									Heading.Append (str [0]);
								}

							}

							// sgsnText.Append("]");
							sgsnText = sgsnText.Replace ("sgsnPDPRecord", "");
							recText = sgsnText.ToString ();
						} else {
							recText = formatter.FormatSGSNRecord (sgsnRecord);
						}
						sgsnRecord.Clear ();
						sgsnRecord.Add (record);
					} else {
						recText = (formatter == null) ? String.Format ("{0,8} > {1} {2}", record.Offset, _status.RecordsOut, record.ToString ()) : formatter.FormatRecord (record);
					}

					if ((filterRegex == null) || (filterRegex.Match (recText).Success)) {
						if (_status.RecordsOut == 1) {
							dstFile.WriteLine (Heading);
						}
						dstFile.WriteLine (recText);
					}

					Math.DivRem (_status.RecordsOut, 1000, out rem);
					//if (rem == 0) _worker.ReportProgress(_status.Percent);
					//if (_worker.CancellationPending)
					//{
					//    break;
					//}
				}

				cdr.Close ();

				//  _logger.AppendLogMessage(_status.RecordsOut.ToString());

				//if (_worker.CancellationPending)
				//{
				//    break;
				//}
				//else
				//{
				//    _status.CdrFilesIn--;
				//    _status.CdrFilesOut++;
				//    _status.Percent = 100;
				//    _worker.ReportProgress(_status.Percent);
				//}
			}

			dstFile.Close ();
			//if (_worker.CancellationPending)
			//{
			//    _status.ResultCode = JobResultCode.CanceledByUser;
			//    _logger.WriteLogMessage("+++ Process aborted by user.", LogLevel.Info);
			//}
			//else
			//{
			_status.ResultCode = JobResultCode.AllOK;
			//  _logger.WriteLogMessage("+++ Decoding is successful done.", LogLevel.Info);
			//}
		}
	}
}
