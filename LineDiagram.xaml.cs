using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KaloriLaskuri
{
    /// <summary>
    /// Name:    Line diagram drawing component
    /// Date:    20.11.2012
    /// Author:  Lasse Löytynoja
    /// Brief:   Draws scaleable line diagram based on given dataset.
    ///          Uses two-dimensional STRING array for input.
    ///          
    ///          Input is taken as strings, but it is required to be 
    ///          in form that can be converted to:
    ///          
    ///          x-axis dataformats: double OR date(UTF yyy-mm-dd)
    ///          y-axis dataformats: double
    ///           
    ///          EXAMPLE:    input[0,1] = "1"       // first x-axis value
    ///                      input[0,2] = "80"      // first y-axis value
    ///                      input[1,1] = "2"       // second x-axis value
    ///                      input[1,2] = "80.5"    // second y-axis value
    ///                      
    ///          NOTE: x-axis values must be in ascending order and user must handle the ordering.
    /// </summary>
     
    public partial class LineDiagram : UserControl
    {
        public enum Axis { x, y };
        public enum PlotArea { StartX, EndX, StartY, EndY, Width, Height, TotalWidth, TotalHeight};
        public double[,] Dataset;
        public int dataFormat;

        /// <summary>ChartColor property</summary>
        /// <value>Sets background color of the chart.</value>

        public Color ChartColor
        {
            get { return (Color)GetValue(ChartColorProperty); }
            set { SetValue(ChartColorProperty, value); }
        }

        public static readonly DependencyProperty ChartColorProperty =
            DependencyProperty.Register("ChartColor", typeof(Color), typeof(LineDiagram), new UIPropertyMetadata(Colors.White));
        
        /// <summary>
        /// LineColor property
        /// </summary>
        /// <value>
        /// Sets the line color of the chart.
        /// </value>

        public Color LineColor
        {
            get { return (Color)GetValue(LineColorProperty); }
            set { SetValue(LineColorProperty, value); }
        }

        public static readonly DependencyProperty LineColorProperty =
            DependencyProperty.Register("LineColor", typeof(Color), typeof(LineDiagram), new UIPropertyMetadata(Color.FromRgb(79, 129, 189)));

        /// <summary>ChartDescription property</summary>
        /// <value>Description that is shown in bottom of the chart</value>   
        public String ChartDescription
        {
            get { return (String)GetValue(ChartDescriptionProperty); }
            set { SetValue(ChartDescriptionProperty, value); }
        }

        public static readonly DependencyProperty ChartDescriptionProperty =
            DependencyProperty.Register("ChartDescription", typeof(String), typeof(LineDiagram), new UIPropertyMetadata(""));

        /// <summary>xAxisName property</summary>
        /// <value>Description of x axis</value>   
        public String xAxisName
        {
            get { return (String)GetValue(xAxisNameProperty); }
            set { SetValue(xAxisNameProperty, value); }
        }

        public static readonly DependencyProperty xAxisNameProperty =
            DependencyProperty.Register("xAxisName", typeof(String), typeof(LineDiagram), new UIPropertyMetadata(""));

        /// <summary>yAxisName property</summary>
        /// <value>Description of y axis</value>  
        public String yAxisName
        {
            get { return (String)GetValue(yAxisNameProperty); }
            set { SetValue(yAxisNameProperty, value); }
        }

        public static readonly DependencyProperty yAxisNameProperty =
            DependencyProperty.Register("yAxisName", typeof(String), typeof(LineDiagram), new UIPropertyMetadata(""));
         

        /// <summary>
        /// Constructor
        /// </summary>
 
        public LineDiagram()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// Sets the dataset from which the diagram is drawn.
        /// </summary>
        /// <param name="input">2-dimensional array of data. See class description for further info.</param>
        public void setData(string[,] input)
        {
            Dataset = new double[input.GetLength(0), 2];

            /* double / double */
            if (dataFormat == 0)
            {
                for (int i = 0; i < input.GetLength(0); i++)
                {
                    try
                    {
                        Dataset[i, 0] = double.Parse(input[i,0]);
                        Dataset[i, 1] = double.Parse(input[i,1]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("{0} exception caught.", e);
                    }
                }
            }
            /* date / double */
            else if (dataFormat == 1)
            {
                DateTime beginning = new DateTime(1, 1, 1);
                for (int i = 0; i < input.GetLength(0); i++)
                {
                    try
                    {
                        DateTime time = DateTime.Parse(input[i, 0]);
                        TimeSpan difference = time - beginning;

                        Dataset[i, 0] = difference.TotalSeconds;
                        Dataset[i, 1] = double.Parse(input[i, 1]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("{0} exception caught.", e);
                    }
                }
            }
        }

        /// <summary>
        /// Defines in which data format the data set is.
        /// </summary>
        /// <param name="format">0 = double/double, 1 = date/double</param>
        public void setDataFormat(int format)
        {
            this.dataFormat = format;
        }
        
        private void vd_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (getPlotVal(PlotArea.Width) > 0 &&
                getPlotVal(PlotArea.Height) > 0)
            {
                drawScale(Axis.x);
                drawScale(Axis.y);
                drawPlot();
            }
        }

        /// <summary>
        /// Handles drawing of the main plot
        /// </summary>
        private void drawPlot()
        {
            /* Calculate node points in x-axis:
             * (<range of x-values> - (xmax - <this value>)) / <range of x-values) * <plot area width> = place in pixels
             *
             * In case that all values are equal, ie. all y axis values are 1 = range will be 1 and normal equation won't work. 
             * Therefore we'll need to set y straight to yMax (or yMin) and skip the equation part.
             */

            double xMax = datasetMax(Axis.x);
            double yMax = datasetMax(Axis.y);
            double xRange = xMax - datasetMin(Axis.x);
            double yRange = yMax - datasetMin(Axis.y);

            Line[] l = new Line[Dataset.GetLength(0)-1];

            /* Dataset is looped through here */

            for (int i = 0; i < Dataset.GetLength(0); i++)
            {
                /* First calculate values for each node. */

                double x;
                double y;

                if (xRange != 0)
                {
                    x = xRange - (xMax - Dataset[i, 0]);
                    double xRel = x / xRange;
                    x = getPlotVal(PlotArea.Width) * xRel;
                }
                else
                {
                    x = xMax;
                }

                if (yRange != 0)
                {
                    y = yRange - (yMax - Dataset[i, 1]);
                    double yRel = y / yRange;
                    y = getPlotVal(PlotArea.Height) * yRel;

                    /* actual value, inverted, margin deducted */
                    y = plotArea.ActualHeight - (y + getPlotVal(PlotArea.StartY));
                }
                else
                {
                    /* range = 0, so always set to center of plot area */
                    y = plotArea.ActualHeight / 2;
                }

                /* actual drawing done downwards here */
                // if first
                if (i == 0)
                {
                    l[i] = new Line();
                    l[i].X1 = getPlotVal(PlotArea.StartX);
                    l[i].Y1 = y;
                }
                // if last
                else if (i == Dataset.GetLength(0)-1)
                {
                    l[i-1].X2 = getPlotVal(PlotArea.EndX);
                    l[i-1].Y2 = y;
                }
                else
                {
                    l[i-1].X2 = x + getPlotVal(PlotArea.StartX);
                    l[i-1].Y2 = y;

                    l[i] = new Line();

                    l[i].X1 = x + getPlotVal(PlotArea.StartX);
                    l[i].Y1 = y;
                }
            }

            /*called in drawScale / x-axis */
            //plotArea.Children.Clear();

            foreach (Line line in l)
            {   
                line.StrokeThickness = 1;
                line.Stroke = new SolidColorBrush(LineColor);
                plotArea.Children.Add(line);
            }
        }

        /// <summary>
        /// Draws x- and y-scales
        /// </summary>
        /// <param name="a">defines which axis gets drawn</param>
        private void drawScale(Axis a)
        {
            /* Calculate scale marker positions.
             * Every 100 of pixels is new marker, 
             * so first divide width with 100 = amount of nodes, 
             * then 1/amount of nodes to get relative value.
             * 
             * After that, the pixels between markers can be calculated.
             * 
             * Like in drawPlot, we need to take account if the range is 0. See
             * explanation in drawPlot's header.
             */

            int xNodes = getPlotVal(PlotArea.Width) / 100;
            double relX = (double)1 / xNodes;
            double xChunk = getPlotVal(PlotArea.Width) * relX;
            double xRange = datasetMax(Axis.x) - datasetMin(Axis.x);

            int yNodes = getPlotVal(PlotArea.Height) / 100;
            double relY = (double)1 / yNodes;
            double yChunk = getPlotVal(PlotArea.Height) * relY;
            double yRange = datasetMax(Axis.y) - datasetMin(Axis.y);

            if (a == Axis.x)
            {   
                Label[] scale = new Label[xNodes+1];
                double[] values = getAxisValueSet(Axis.x, xNodes+1);
                double wdtUnit = getPlotVal(PlotArea.StartX);

                plotArea.Children.Clear();
                xAxisArea.Children.Clear();

                if (xRange != 0)
                {
                    for (int i = 0; i <= xNodes; i++)
                    {
                        /* Scale labels and markers */
                        Line l = new Line();
                        scale[i] = new Label();

                        /* do seconds to date conversion here, depending on the dataformat */

                        if (dataFormat == 1)
                        {
                            DateTime date = new DateTime(1, 1, 1);
                            date = date.AddSeconds(values[i]);
                            scale[i].Content = date.Year + "-" + date.Month + "-" + date.Day;                         
                        }
                        else
                        {
                            scale[i].Content = values[i];
                        }

                        if (i == 0)
                        {
                            Canvas.SetLeft(scale[i], getPlotVal(PlotArea.StartX));
                            wdtUnit += xChunk;

                            l.X1 = getPlotVal(PlotArea.StartX);
                            l.X2 = l.X1;
                            l.Y1 = 0;
                            l.Y2 = 5;
                        }
                        else if (i == xNodes)
                        {
                            Canvas.SetLeft(scale[i], getPlotVal(PlotArea.EndX));

                            l.X1 = getPlotVal(PlotArea.EndX);
                            l.X2 = l.X1;
                            l.Y1 = 0;
                            l.Y2 = 5;
                        }
                        else
                        {
                            Canvas.SetLeft(scale[i], wdtUnit);
                            l.X1 = wdtUnit;
                            l.X2 = l.X1;
                            l.Y1 = 0;
                            l.Y2 = 5;
                            wdtUnit += xChunk;
                        }

                        l.StrokeThickness = 1;
                        l.Stroke = new SolidColorBrush(Colors.Black);

                        xAxisArea.Children.Add(scale[i]);
                        xAxisArea.Children.Add(l);
                    }
                }
                /* if range = 0, do not scale - draw only one marker */
                else
                {
                    Line l = new Line();
                    scale[0] = new Label();

                    scale[0].Content = values[0];
                    Canvas.SetLeft(scale[0], getPlotVal(PlotArea.TotalWidth)/2);

                    l.X1 = getPlotVal(PlotArea.TotalWidth) / 2;
                    l.X2 = l.X1;
                    l.Y1 = 0;
                    l.Y2 = 5;
                    l.StrokeThickness = 1;
                    l.Stroke = new SolidColorBrush(Colors.Black);

                    xAxisArea.Children.Add(scale[0]);
                    xAxisArea.Children.Add(l);
                }

                /* Add also a borderline */

                Line xBorder = new Line();
                xBorder.X1 = 0;
                xBorder.X2 = getPlotVal(PlotArea.TotalWidth);
                xBorder.Y1 = 0;
                xBorder.Y2 = 0;
                xBorder.StrokeThickness = 1;
                xBorder.Stroke = new SolidColorBrush(Colors.DarkGray);
                
                Label xName = new Label();
                xName.Content = xAxisName;

                xAxisArea.Children.Add(xName);
                Canvas.SetBottom(xName, 0);
                Canvas.SetRight(xName, 0);
                xAxisArea.Children.Add(xBorder);
            }

            if (a == Axis.y)
            {
                /* Note: this also handles scale drawing in main plotArea -canvas!
                 * Why? Because correct y values are derived here.
                 */

                Label[] scale = new Label[yNodes+1];
                double[] values = getAxisValueSet(Axis.y, yNodes+1);
                double hgtUnit = getPlotVal(PlotArea.StartY);

                yAxisArea.Children.Clear();

                if (xRange != 0)
                {
                    for (int i = 0; i <= yNodes; i++)
                    {
                        /* Scale labels and markers */
                        Line l = new Line();
                        Line lPlot = new Line();
                        scale[i] = new Label();

                        /* measurements... */
                        double rightBorder = yAxisArea.ActualWidth;
                        double paRightBorder = plotArea.ActualWidth;

                        if (i == 0)
                        {
                            scale[i].Content = values[i];
                            Canvas.SetBottom(scale[i], getPlotVal(PlotArea.StartY));
                            hgtUnit += yChunk;

                            /* scale marker */
                            l.X1 = rightBorder;
                            l.X2 = rightBorder - 5;
                            l.Y1 = getPlotVal(PlotArea.StartY);
                            l.Y2 = l.Y1;

                            /* scale line on plotArea */
                            lPlot.X1 = 0;
                            lPlot.X2 = paRightBorder;
                            lPlot.Y1 = getPlotVal(PlotArea.StartY);
                            lPlot.Y2 = lPlot.Y1;
                        }
                        else if (i == yNodes)
                        {
                            scale[i].Content = values[i];
                            Canvas.SetBottom(scale[i], getPlotVal(PlotArea.EndY));

                            /* scale marker */
                            l.X1 = rightBorder;
                            l.X2 = rightBorder - 5;
                            l.Y1 = getPlotVal(PlotArea.EndY);
                            l.Y2 = l.Y1;

                            /* scale line on plotArea */
                            lPlot.X1 = 0;
                            lPlot.X2 = paRightBorder;
                            lPlot.Y1 = getPlotVal(PlotArea.EndY);
                            lPlot.Y2 = lPlot.Y1;
                        }
                        else
                        {

                            scale[i].Content = values[i];
                            Canvas.SetBottom(scale[i], hgtUnit);
                            /* scale marker */
                            l.X1 = rightBorder;
                            l.X2 = rightBorder - 5;
                            l.Y1 = hgtUnit;
                            l.Y2 = l.Y1;

                            /* scale line on plotArea */
                            lPlot.X1 = 0;
                            lPlot.X2 = paRightBorder;
                            lPlot.Y1 = hgtUnit;
                            lPlot.Y2 = lPlot.Y1;

                            hgtUnit += yChunk;
                        }

                        l.StrokeThickness = 1;
                        l.Stroke = new SolidColorBrush(Colors.Black);
                        lPlot.StrokeThickness = 1;
                        lPlot.Stroke = new SolidColorBrush(Colors.LightGray);

                        yAxisArea.Children.Add(scale[i]);
                        yAxisArea.Children.Add(l);
                        plotArea.Children.Add(lPlot);
                    }
                }
                /* if range = 0, do not scale - draw only one marker */
                else
                {
                    Line l = new Line();
                    scale[0] = new Label();

                    scale[0].Content = values[0];
                    Canvas.SetBottom(scale[0], getPlotVal(PlotArea.TotalHeight) / 2);

                    l.X1 = yAxisArea.ActualWidth;
                    l.X2 = l.X1 - 5;
                    l.Y1 = getPlotVal(PlotArea.TotalHeight) / 2;
                    l.Y2 = l.Y1;
                    l.StrokeThickness = 1;
                    l.Stroke = new SolidColorBrush(Colors.Black);

                    yAxisArea.Children.Add(scale[0]);
                    yAxisArea.Children.Add(l);
                }
            }
            /* Add also a borderline and label for the axis*/

            Line yBorder = new Line();
            yBorder.X1 = yAxisArea.ActualWidth;
            yBorder.X2 = yAxisArea.ActualWidth;
            yBorder.Y1 = 0;
            yBorder.Y2 = getPlotVal(PlotArea.TotalHeight);
            yBorder.StrokeThickness = 1;
            yBorder.Stroke = new SolidColorBrush(Colors.DarkGray);

            Label yName = new Label();
            yName.Content = yAxisName;
            
            margin.Children.Add(yName);
            yAxisArea.Children.Add(yBorder);
        }

        /// <summary>
        /// Calculates and converts plot area measurements
        /// </summary>
        /// <param name="p">Selects measurement, uses enum PlotArea</param>
        /// <returns>measurement</returns>
        private int getPlotVal(PlotArea p)
        {
            int val = 0;

            switch (p)
            {
                case PlotArea.StartX:
                    val = 50;
                    break;
                case PlotArea.EndX:
                    val = Convert.ToInt16(plotArea.ActualWidth - 50);
                    break;
                case PlotArea.StartY:
                    val = 25;
                    break;
                case PlotArea.EndY:
                    val = Convert.ToInt16(plotArea.ActualHeight - 25);
                    break;
                case PlotArea.Width:
                    val = Convert.ToInt16(plotArea.ActualWidth - 100);
                    break;
                case PlotArea.Height:
                    val = Convert.ToInt16(plotArea.ActualHeight - 50);
                    break;
                case PlotArea.TotalWidth:
                    val = Convert.ToInt16(plotArea.ActualWidth);
                    break;
                case PlotArea.TotalHeight:
                    val = Convert.ToInt16(plotArea.ActualHeight);
                    break;
                default:
                    break;
            }
            return val;
        }

        /// <summary>
        /// Calculates values that are shown in axis scale
        /// </summary>
        /// <param name="a">Selects the axis</param>
        /// <param name="amount">In how many sections the axis is divided</param>
        /// <returns>array of scale values</returns>
        private double[] getAxisValueSet(Axis a, int amount)
        {   
            double max = datasetMax(a);
            double min = datasetMin(a);
            double tot = max - min;
            double chunk;
            double[] axis;

            axis = new double[amount];
            chunk = tot /(amount-1);
            
            // set first and last as min and max
            axis[0] = min;
            axis[amount-1] = max;
            
            // fill gaps only if there's something to fill
            if (amount > 2)
            {
                for(int i = 1; i < amount-1; i++)
                {
                    axis[i] = Math.Round(axis[i-1] + chunk, 2);
                }
            }
            return axis;
        }

        /// <summary>
        /// Searches maximum value from dataset
        /// </summary>
        /// <param name="a">selects axis</param>
        /// <returns>maximum value</returns>
        private double datasetMax(Axis a)
        {
            int col = (a == Axis.x) ? 0 : 1; 
            double max = Dataset[0,col];

            for (int i = 0; i < Dataset.GetLength(0); i++)
            {
                if (max < Dataset[i,col])
                {
                    max = Dataset[i,col];
                }
            }

            return max;
        }

        /// <summary>
        /// Searches minimum value from dataset
        /// </summary>
        /// <param name="a">selects axis</param>
        /// <returns>minimum value</returns>
        private double datasetMin(Axis a)
        {
            int col = (a == Axis.x) ? 0 : 1; 
            double min = Dataset[0,col];

            for (int i = 0; i < Dataset.GetLength(0); i++)
            {
                if (min > Dataset[i,col])
                {
                    min = Dataset[i,col];
                }
            }

            return min;
        }
    }
}
