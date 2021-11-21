using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Numerics; //Para numeros complejos
using MathNet.Numerics.IntegralTransforms; //Para FFT
using System.Windows.Forms.DataVisualization.Charting; //Para charts
using MathNet.Numerics; //Para generar la señal de prueba
//using System.Linq;//Para obtener maximo valorde array


namespace Comunicacionserial
{

    public partial class Form1 : Form
    {
        Boolean temblor=false;

        string portselec;
        static int N= 256;//Tamaño de la muestra de 256

        static int Fs = 25; //Frecuencia de muestreo
        static int m = 25;    //Numero de muestras
        Complex[] x = new Complex[N];
        Complex[] x0 = new Complex[m+1];
        //Señal de prueba nadamas
        static readonly double[] entrada = Generate.Sinusoidal(m, 25, 6, 2);
        double[] magX = new double[N /2];

        public Form1()
        {   
            InitializeComponent();
            string[] puertos = SerialPort.GetPortNames();
            foreach (string mostrar in puertos)
            {
                comboBox1.Items.Add(mostrar);
            }
            //Leyendas de los graficos
            chart1.ChartAreas[0].AxisX.Title = "Tiempo";
            chart1.ChartAreas[0].AxisX.TitleFont = new Font("Arial", 13.0f);

            chart1.ChartAreas[0].AxisY.Title = "m/s^2";
            chart1.ChartAreas[0].AxisY.TitleFont = new Font("Arial", 13.0f);

            chart2.ChartAreas[0].AxisX.Title = "Hz";
            chart2.ChartAreas[0].AxisX.TitleFont = new Font("Arial", 13.0f);

            chart2.ChartAreas[0].AxisY.Title = "dB";
            chart2.ChartAreas[0].AxisY.TitleFont = new Font("Arial", 13.0f);
            //Grafico la señal  que cree
            for (int n = 0; n < m; n++)
            {
                double tiempo = n *1.0/ Fs;
                chart1.Series[0].Points.AddXY(tiempo, entrada[n]);
            }
            //Arreglo que guarda los datos
            //Se rellena de los valores medidos
            for (int i = 0; i <m; i++)
            {
                x[i] = new Complex(entrada[i], 0);
            }

            PlotFFT();
        }

        public void PlotFFT()
        {
            //Se aplica ventana Hanning
            for (int i = 0; i < m; i++)
            {
                x[i] = x[i] * (0.5 - (0.5 * Math.Cos((2 * Math.PI * i) / m - 1)));
            }

            //Se aplica Zero padding
            for (int i = m; i < N; i++)
            {
                x[i] = new Complex(0, 0);
            }

            chart2.Series[0].Points.Clear();

            //Forward Fourier convierte el tiempo en frecuencia
            Fourier.Forward(x, FourierOptions.NoScaling); //No se se escalan los resultados

            //Se toma solo unas muestra del arreglo ya transformado
            for (int i = 0; i < (N/2); i++)
            {
                //Se obtiene la magnitud de la Transformada de Fourier =abs[sqrt(r^2+i^2)]
                magX [i]= Math.Abs(Math.Sqrt(Math.Pow(x[i].Real, 2) + Math.Pow(x[i].Imaginary, 2)));
                if(magX[i]==0)
                {
                    magX[0] = 0.001;  
                }
                double dB = 10*Math.Log10(magX[i]);

                //Determinación de Hz 
                double Hz = (i * Fs/ 256.0);
                Hz = Math.Round(Hz, 2);
                chart2.Series[0].Points.AddXY(Hz, dB); //Multiplicar i *fmuestreo
            }
            double valMax= magX.Max();
            double binMax=0;
            for (int i = 1; i < (N / 2); i++)
            {
                if (valMax == magX[i])
                {
                    binMax = i;
                    i = N;
                }
            }
            double frecuencia = binMax*Fs/N;
            frecuencia =Math.Round(frecuencia, 2);
            //Imprimimos la frecuencia
            label7.Text = frecuencia.ToString();
        }

        //Cada que necesite actualizar mandar a llamar la funcion PlotFFT();
        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            serialPort1.Close();
            serialPort1.Dispose();
            portselec= comboBox1.Text;
            serialPort1.PortName=portselec;
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            serialPort1.Close();
            serialPort1.Dispose(); 
            Close();
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        Boolean i = false;
        int c = 0;
        public void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //inicia el timer1 para que sea cero cuando se recibe el primer dato

            if (inicio == true)
            {
                if (i == false)
                {
                    t = 0;
                    i = true;
                }
                var dato = serialPort1.ReadLine();
                double numero = Convert.ToDouble(dato);
                double tap = Math.Round(((numero * 4) / 65535), 2);
                chart1.Series[0].Points.AddXY(t, tap);
                label4.Text = tap.ToString();

                //Arreglo que guarda los datos
                //Se rellena de los valores medidos
                if (c<= m)//Si c es menor o igual a 25 
                {
                    x0[c] = new Complex(tap, 0);  //Se rellena la posicion desde 0 a 25 hasta que c ==26
                    c ++;
                }
                if (c>m) //Si c>25
                {
                    c = 25;
                    for (int i = 0; i < m; i++) //Recorrimiento desde 0 hasta 24 tomando valor de 25
                    {
                        x[i] = x0[i+1];
                        x0[i] = x[i];
                        x0[i + 1] = 0;
                    }
                }
               // PlotFFT();
            }
        } 
        

        private void label5_Click(object sender, EventArgs e)
        {

        }

        double t = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            t = t+0.04;
            if (inicio == true && temblor==true)
            {
                PlotFFT();
            }
            if (t==20.0)
            {
                inicio = false;
                serialPort1.Write("S");
            }

        }

        Boolean inicio= false;
        private void button2_Click(object sender, EventArgs e)//Boton para iniciar recepcion de datos
        {
            inicio = true;
            chart1.Series[0].Points.Clear();
            chart2.Series[0].Points.Clear();
            serialPort1.Write("R");
        }

        private void button3_Click(object sender, EventArgs e)//Boton para detener la recepción de datos
        {
            inicio = false;
            serialPort1.Write("S");
        }

        private void button5_Click(object sender, EventArgs e) //Boton para desconectar puerto
        {
            serialPort1.Close();
            serialPort1.Dispose();
            label3.Text = "PUERTO DESCONECTADO";
        }

        private void button6_Click(object sender, EventArgs e) //Boton para conectar puerto
        {
            serialPort1.Open();
            CheckForIllegalCrossThreadCalls = false;
            if (serialPort1.IsOpen == true)
            {
                label3.Text = "PUERTO CONECTADO";
            }
        }

        private void button4_Click(object sender, EventArgs e) //Limpiar grafico
        {
            chart1.Series[0].Points.Clear();
            i = false;
            inicio = false;
        }

        private void button7_Click(object sender, EventArgs e) //Actualizar puertosdisponibles
        {
            string[] puertos = SerialPort.GetPortNames();
            foreach (string mostrar in puertos)
            {
                comboBox1.Items.Add(mostrar);
            }
        }

        private void chart2_Click(object sender, EventArgs e)
        {
              
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            temblor = true;
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            temblor = false;
            label7.Text = ("-");
        }
    }
}
