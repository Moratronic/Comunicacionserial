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
//Comentario de prueba

namespace Comunicacionserial
{

    public partial class Form1 : Form
    {
        Boolean temblor=false;

        string portselec;
        static int N= 512;//Tamaño de la muestra a aplicar FFT
        string tipoprueba;

        static int Fs = 25; //Frecuencia de muestreo
        static int m = 16;    //Numero de muestras
        static int fc = 7;    //Frecuencia de corte
        static int mcut = 0;  //Bin de corte
        Complex[] x = new Complex[Fs*timelect+1];     //cadena de datos leidos
        Complex[] x0 = new Complex[N-m];    //Cadena de ceros
        Complex[] X = new Complex[N];       //Cadena para aplicar FFT
        double[] Filtro = new double[N/2];  //Filtro FIR
        double[] magX = new double[N /2];
        static int timelect = 10;
        //Señal de prueba nadamas
        static readonly double[] entrada = Generate.Sinusoidal(5000, 25, 6, 2);


        double t = 0;   //Variable que cuenta el tiempo
                        // int g = 0;    
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

            chart1.ChartAreas[0].AxisY.Title = "Fuerza (G)";
            chart1.ChartAreas[0].AxisY.TitleFont = new Font("Arial", 13.0f);

            chart2.ChartAreas[0].AxisY.Title = "Frecuencia (Hz)";
            chart2.ChartAreas[0].AxisY.TitleFont = new Font("Arial", 13.0f);

            chart2.ChartAreas[0].AxisX.Title = "Tiempo";
            chart2.ChartAreas[0].AxisX.TitleFont = new Font("Arial", 13.0f);
            chart1.ChartAreas[0].AxisX.Interval = 1;
            chart1.ChartAreas[0].AxisY.Interval = 0.5;
            chart2.ChartAreas[0].AxisX.Interval = 1;
            chart2.ChartAreas[0].AxisY.Interval = 0.5;



            mcut = fc * N / Fs;     //SE determina bin de corte
            for (int n = 0; n < N/2; n++)//Asignacion de 1 a array de corte
            {
                Filtro [n]= 0;
                if (n<= mcut)
                {
                    Filtro[n] = 1;
                }
            }

            //Grafico la señal  que cree
            for (int n = 0; n < 200; n++)
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
            for (int counter = 0; counter <= (Fs * timelect); counter++)//Desde 0 a 250
            {
                if (counter < m)//Si counter menor a 16
                {
                    for (int i = 0; i <=counter; i++)
                    {
                        X[i] = x[i];
                    }
                    x0.CopyTo(X, counter + 1);
                }
                else {//Si counter vale posicion 16
                    for (int i = 0; i < m; i++)
                    {
                        X[i] = x[i +1+counter-m];
                    }
                }

                //Se aplica Zero padding
                x0.CopyTo(X, m);

                //Se aplica ventana Hanning 
                for (int i = 0; i < m; i++)
                {
                    //Console.Write(X[i]);
                    X[i] = X[i] * (0.5 - (0.5 * Math.Cos((2 * Math.PI * i) / m - 1)));
                }

                //Forward Fourier convierte el tiempo en frecuencia
                Fourier.Forward(X, FourierOptions.NoScaling); //No se se escalan los resultados
                                                              //Se toma solo unas muestra del arreglo ya transformado
                for (int i = 0; i < (N / 2); i++)
                {
                    //Se obtiene la magnitud de la Transformada de Fourier =abs[sqrt(r^2+i^2)] y se multiplica por el filtro
                    magX[i] = Math.Abs(Math.Sqrt(Math.Pow(X[i].Real, 2) + Math.Pow(X[i].Imaginary, 2)));
                    magX[i] = magX[i] * Filtro[i] * 1.0;

                    if (magX[i] == 0)
                    {
                        magX[i] = 0.01;
                    }
                    double dB = 20 * Math.Log10(magX[i]);//Revisar si no es *100
                                                         //Determinación de Hz 
                    double Hz = (i * Fs * 1.0 / N);
                    Hz = Math.Round(Hz, 2);
                    //                chart2.Series[0].Points.AddXY(Hz, dB); //Multiplicar i *fmuestreo
                }
                double valMax = magX.Max();
                double binMax = 0;
                for (int i = 1; i < (N / 2); i++)
                {
                    if (valMax == magX[i])
                    {
                        binMax = i;
                        i = N;
                    }
                }
                double frecuencia = binMax * Fs / N;
                frecuencia = Math.Round(frecuencia, 2);
                chart2.Series[0].Points.AddXY(counter*0.04+0.04,frecuencia); //Multiplicar i *fmuestreo
                                                              //Imprimimos la frecuencia
                label7.Text = frecuencia.ToString();

            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) //Seleccion de puerto COM
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

        int g = 0;
        double numero;
        public void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)//Recepcion de datos
        {
            if (inicio == true)
            {
                    bool input = double.TryParse(serialPort1.ReadLine(), out numero);
                    if (!input)
                    {
                        numero = 0;
                    }
                    t = t + 0.04;
                    double tap = Math.Round(((numero * 4.0) / 65535), 2);
                    chart1.Series[0].Points.AddXY(t, tap);
                    label4.Text = tap.ToString();

                //Arreglo que guarda los datos
                //Se rellena de los valores medidos
                if (temblor == true)
                {
                    x[g] = tap;
                }
                g++;
                //   Console.WriteLine(g);
                if (t > timelect)
                    {
                        inicio = false;
                        serialPort1.Write("S");
                        t = 0;
                        g = 0;
                        if (temblor == true)
                        {
                            PlotFFT();
                        }
                    }
            }
        } 
        

        private void label5_Click(object sender, EventArgs e)
        {

        }

        Boolean inicio= false;
        private void button2_Click(object sender, EventArgs e)//Boton para iniciar recepcion de datos
        {
            inicio = true;
            chart1.Series[0].Points.Clear();
            chart2.Series[0].Points.Clear();
            serialPort1.Write(tipoprueba);
            t = 0;
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
            chart2.Series[0].Points.Clear();
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

        public void chart2_Click(object sender, EventArgs e)
        {
              
        }

        public void chart1_Click(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)//Seleccion de tipo de prueba
        {
            if (comboBox2.Text == "Prueba TAP")
            {
                temblor = false;
                label7.Text = ("-");
                tipoprueba = "A";
            }

            if (comboBox2.Text == "Prueba TEP")
            {
                temblor = true;
                for (int i = 0; i <=Fs*timelect; i++) //Se llena de ceros el arreglo
                {
                    x[i] = 0;
                    tipoprueba = "E";
                }
            }
        }
    }
}
