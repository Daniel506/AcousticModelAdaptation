package com.example.asronlinetraining;

import java.io.BufferedInputStream;
import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.ObjectInputStream;
import java.io.ObjectOutputStream;
import java.net.HttpURLConnection;
import java.net.MalformedURLException;
import java.net.Socket;
import java.net.URL;
import java.net.UnknownHostException;
import java.util.zip.ZipEntry;
import java.util.zip.ZipInputStream;

import com.example.asronlinetraining.R;

import android.os.Bundle;
import android.os.Environment;
import android.os.Handler;
import android.os.Message;
import android.app.Activity;
import android.app.Dialog;
import android.util.Log;
import android.view.Menu;
import android.view.View;
import android.view.View.OnClickListener;
import android.view.Window;
import android.widget.Button;
import android.widget.ProgressBar;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

public class UploadFiles extends Activity {

	private Button btnUpload;
	private Button btnDownload;
	private Button btnUnzip;
	private Button btnUploadFinished;
	private Spinner spinCommands;
	private File[] list;

	ProgressBar pb;
	Dialog dialog;
	int downloadedSize = 0;
	int totalSize = 0;
	TextView cur_val;
	String download_file_path = "http://10.0.2.2:60177/NewModel/new-model.zip";

	TextView serverMessage;
	Thread m_objThreadClient;
	Socket clientSocket;

	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		setContentView(R.layout.activity_upload_files);

		btnUpload = (Button) findViewById(R.id.btnUpload);
		btnDownload = (Button) findViewById(R.id.btnDownload);
		btnUnzip = (Button) findViewById(R.id.btnUnzip);
		spinCommands = (Spinner) findViewById(R.id.spinnerCommands);
		btnUploadFinished = (Button) findViewById(R.id.btnUploadFinish);
		serverMessage = (TextView) findViewById(R.id.serverResponse);

		File files = new File(Environment.getExternalStorageDirectory()
				.toString() + "/asr-model");
		list = files.listFiles();
		
		btnUpload.setOnClickListener(new OnClickListener() {
			
			@Override
			public void onClick(View v) {
				String selectedItem = spinCommands.getSelectedItem().toString();

				serverMessage.setText("Status: uploading recordings ...");
				
				for (int i = 0; i < list.length; i++) {
					String fileName = list[i].getName();
					
					// if (fileName.contains(selectedItem)) {
					String pom = list[i].getAbsolutePath();
					UploadFile(list[i].getAbsolutePath());
					// }
				}
				
				serverMessage.setText("Status: Recordings uploaded successfully!");
			}
		});

		btnUploadFinished.setOnClickListener(new OnClickListener() {

			@Override
			public void onClick(View v) {
				serverMessage.setText("Status: Adaptation in progress ...");	
				Start(v);
				serverMessage.setText("Status: Adaptation finished!");	
			}
		});

		btnDownload.setOnClickListener(new OnClickListener() {

			@Override
			public void onClick(View v) {

				showProgress(download_file_path);
				
				serverMessage.setText("Status: downloading new model ...");	

				new Thread(new Runnable() {
					public void run() {
						downloadFile();
						try {
							clientSocket = new Socket("10.0.2.2", 2001);
							ObjectOutputStream oos = new ObjectOutputStream(
									clientSocket.getOutputStream());
							oos.writeObject("+DownloadCompleted+");
							Message serverMessageResponse = Message.obtain();
						} catch (UnknownHostException e) {
							// TODO Auto-generated catch block
							e.printStackTrace();
						} catch (IOException e) {
							// TODO Auto-generated catch block
							e.printStackTrace();
						}
					}
				}).start();
				
				serverMessage.setText("Status: New Model downloaded!");	
			}
		});

		btnUnzip.setOnClickListener(new OnClickListener() {

			@Override
			public void onClick(View v) {
				try {

					unzip(Environment.getExternalStorageDirectory().toString()
							+ "/new-model/new-model.zip", Environment
							.getExternalStorageDirectory().toString()
							+ "/new-model/");

					/*
					 * unpackZip(Environment.getExternalStorageDirectory()
					 * .toString() + "/new-model/", "new-model.zip");
					 */
					
					serverMessage.setText("New model creation finished!");
					
					File file = new File(Environment.getExternalStorageDirectory().toString() + "/new-model/new-model.zip");
					file.delete();

				} catch (Exception e) {
					e.printStackTrace();
				}

			}
		});
	}

	public void Start(View view) {
		m_objThreadClient = new Thread(new Runnable() {
			boolean receivedResponse = false;
			public void run() {
				try {
					
					/*for (int i = 0; i < list.length; i++) {
						UploadFile(list[i].getAbsolutePath());
					}*/
					clientSocket = new Socket("10.0.2.2", 2001);
					ObjectOutputStream oos = new ObjectOutputStream(
							clientSocket.getOutputStream());
					oos.writeObject("+"
							+ spinCommands.getSelectedItem().toString() + "+");
					Message serverMessageResponse = Message.obtain();

					while (!Thread.currentThread().isInterrupted()) {
						if (!receivedResponse) {
							ObjectInputStream ois = new ObjectInputStream(
									clientSocket.getInputStream());
							String strMessage = (String) ois.readObject();
							serverMessageResponse.obj = strMessage;
							mHandler.sendMessage(serverMessageResponse);
							ois.close();
							oos.close();
							receivedResponse = true;
							
							/*downloadFile();
							
							unzip(Environment.getExternalStorageDirectory().toString()
									+ "/new-model/new-model.zip", Environment
									.getExternalStorageDirectory().toString()
									+ "/new-model/");*/	
							break;
						}
					}
					
				} catch (Exception e) {
					// TODO Auto-generated catch block
					e.printStackTrace();
				}

			}
		});

		m_objThreadClient.start();

	}

	Handler mHandler = new Handler() {
		@Override
		public void handleMessage(Message msg) {
			messageDisplay(msg.obj.toString());
		}
	};

	public void messageDisplay(String servermessage) {
		serverMessage.setText("" + servermessage);
	}

	void downloadFile() {

		try {
			URL url = new URL(download_file_path);
			HttpURLConnection urlConnection = (HttpURLConnection) url
					.openConnection();

			urlConnection.setRequestMethod("GET");
			urlConnection.setDoOutput(true);

			// connect
			urlConnection.connect();

			// set the path where we want to save the file
			File SDCardRoot = Environment.getExternalStorageDirectory();
			File destDir = new File(SDCardRoot.getAbsolutePath() + "/new-model");
			// create a new file, to save the downloaded file
			File file = new File(destDir, "new-model.zip");

			FileOutputStream fileOutput = new FileOutputStream(file);

			// Stream used for reading the data from the internet
			InputStream inputStream = urlConnection.getInputStream();

			// this is the total size of the file which we are downloading
			totalSize = urlConnection.getContentLength();

			runOnUiThread(new Runnable() {
				public void run() {
					pb.setMax(totalSize);
				}
			});

			// create a buffer...
			byte[] buffer = new byte[1024];
			int bufferLength = 0;

			while ((bufferLength = inputStream.read(buffer)) > 0) {
				fileOutput.write(buffer, 0, bufferLength);
				downloadedSize += bufferLength;
				// update the progressbar //
				runOnUiThread(new Runnable() {
					public void run() {
						pb.setProgress(downloadedSize);
						float per = ((float) downloadedSize / totalSize) * 100;
						cur_val.setText("Downloaded " + downloadedSize
								+ "KB / " + totalSize + "KB (" + (int) per
								+ "%)");
					}
				});
			}
			// close the output stream when complete //
			fileOutput.close();
			runOnUiThread(new Runnable() {
				public void run() {
					// pb.dismiss(); // if you want close it..
				}
			});

		} catch (final MalformedURLException e) {
			showError("Error : MalformedURLException " + e);
			e.printStackTrace();
		} catch (final IOException e) {
			showError("Error : IOException " + e);
			e.printStackTrace();
		} catch (final Exception e) {
			showError("Error : Please check your internet connection " + e);
		}
	}

	void showError(final String err) {
		runOnUiThread(new Runnable() {
			public void run() {
				Toast.makeText(UploadFiles.this, err, Toast.LENGTH_LONG).show();
			}
		});
	}

	void showProgress(String file_path) {
		dialog = new Dialog(UploadFiles.this);
		dialog.requestWindowFeature(Window.FEATURE_NO_TITLE);
		dialog.setContentView(R.layout.myprogressdialog);
		dialog.setTitle("Download Progress");

		TextView text = (TextView) dialog.findViewById(R.id.tv1);
		text.setText("Downloading file from ... " + file_path);
		cur_val = (TextView) dialog.findViewById(R.id.cur_pg_tv);
		cur_val.setText("Starting download...");
		dialog.show();

		pb = (ProgressBar) dialog.findViewById(R.id.progress_bar);
		pb.setProgress(0);
		pb.setProgressDrawable(getResources().getDrawable(
				R.drawable.green_progress));
	}

	@Override
	public boolean onCreateOptionsMenu(Menu menu) {
		// Inflate the menu; this adds items to the action bar if it is present.
		getMenuInflater().inflate(R.menu.upload_files, menu);
		return true;
	}

	public void UploadFile(String filePath) {
		try {
			// Set your file path here
			FileInputStream fstrm = new FileInputStream(filePath);

			String fileName = filePath.split("/")[filePath.split("/").length - 1];

			// Set your server page url (and the file title/description)
			HttpFileUpload hfu = new HttpFileUpload(
					"http://10.0.2.2:60177/Upload.aspx", fileName,
					"my file description");

			hfu.Send_Now(fstrm);

		} catch (Exception e) {
			// Error: File not found
			e.printStackTrace();
		}
	}

	public static void unzip(String zipFile, String location)
			throws IOException {
		try {
			File f = new File(location);
			if (!f.isDirectory()) {
				f.mkdirs();
			}
			ZipInputStream zin = new ZipInputStream(
					new FileInputStream(zipFile));
			try {
				ZipEntry ze = null;
				while ((ze = zin.getNextEntry()) != null) {
					String path = location + ze.getName();

					if (ze.isDirectory()) {
						File unzipFile = new File(path);
						if (!unzipFile.isDirectory()) {
							unzipFile.mkdirs();
						}
					} else {
						FileOutputStream fout = new FileOutputStream(path,
								false);
						try {
							for (int c = zin.read(); c != -1; c = zin.read()) {
								fout.write(c);
							}
							zin.closeEntry();
						} finally {
							fout.close();
						}
					}
				}
			} finally {
				zin.close();
			}
		} catch (Exception e) {
			Log.e("ASROnlineTraining", "Unzip exception", e);
		}
	}

	private boolean unpackZip(String path, String zipname) {
		InputStream is;
		ZipInputStream zis;
		try {
			is = new FileInputStream(path + zipname);
			zis = new ZipInputStream(new BufferedInputStream(is));
			ZipEntry ze;

			while ((ze = zis.getNextEntry()) != null) {
				ByteArrayOutputStream baos = new ByteArrayOutputStream();
				byte[] buffer = new byte[1024];
				int count;

				String filename = ze.getName();
				FileOutputStream fout = new FileOutputStream(path + filename);

				// reading and writing
				while ((count = zis.read(buffer)) != -1) {
					baos.write(buffer, 0, count);
					byte[] bytes = baos.toByteArray();
					fout.write(bytes);
					baos.reset();
				}

				fout.close();
				zis.closeEntry();
			}

			zis.close();
		} catch (IOException e) {
			e.printStackTrace();
			return false;
		}

		return true;
	}

}
