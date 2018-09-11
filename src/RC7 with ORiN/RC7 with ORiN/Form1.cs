using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Diagnostics;		// For Process handling
using ORiN2.interop.CAO;        // Provide a path for the code below to the CAORCW dll that is in the References are of the projects.

namespace RC7_with_ORiN
{
    public partial class Form1 : Form
    {
        // Global Variables

        private CaoEngine ORiN_Engine;
        private CaoController RC7Controller;
        private CaoVariable RC7variable_S10;
        private CaoTask RC7Task_RobSlave;

        private int incrementValue = 0;

        public Form1()
        {
            //This constructor for the graphical interface is called automatically upon program startup before the form is visible to the user
            InitializeComponent();

            //Disable the buttons to prevent exceptions from the user clicking them out of order
            button_ReadVariable.Enabled = false;
            button_WriteVariable.Enabled = false;
            button_Close.Enabled = false;

            KillCaoExecutableIfItIsAlreadyRunning();	// This is required to prevent lockup in the event that this program is not properly shut down

            ORiN_Engine = new CaoEngine();				// This starts a separate program called "CAO.exe" which allows this program to talk with RC7 controllers
        }

        private static void KillCaoExecutableIfItIsAlreadyRunning()
        {
            // Kill the CAO.exe process in case it wasn't properly shut down last time
            // For example, in case the user ran this code and then aborted it instead of shutting it down normally using the Close/Disconnect method below
            Process[] caoProcesses = Process.GetProcessesByName("CAO");	// Retrieve all processes that have the name "CAO"

            if ((caoProcesses != null) && (caoProcesses.Length > 0))	// If there is at least one "CAO" process...
                caoProcesses[0].Kill();	// Kill the first process
        }

        private void button_Connect_Click(object sender, EventArgs e)
        {
            button_Connect.Enabled = false;

            // Create a link to the RC7 controller via the engine (CAO.exe)
            
            RC7Controller = ORiN_Engine.Workspaces.Item(0).AddController(
                                "Sample",						// This is the name of the connection to the RC7 controller; Pass an empty string to have the controller auto-generate a name
                                "CaoProv.DENSO.NetwoRC",	// This is the name of the DENSO ORiN provider for RC7; It must be exactly as shown
                                "",             			// This is the name of the machine executing the DENSO ORiN provider (usually the machine running this program)
                                "Conn=eth:192.168.0.2");	// This is the IP address of the physical RC7 controller

            //In order to perform motion commands on the pc side you should start the RobSlave.pac program on the RC7
            RC7Task_RobSlave = RC7Controller.AddTask("RobSlave", "");
            RC7Task_RobSlave.Start(1, "");
            

            // Add a local variable that is linked to string variable S10 on the physical RC7 controller
            RC7variable_S10 = RC7Controller.AddVariable("S10");

            button_ReadVariable.Enabled = true;
            button_WriteVariable.Enabled = true;
            button_Close.Enabled = true;
        }

        private void button_ReadVariable_Click(object sender, EventArgs e)
        {
            ReadVariable();
        }

        private void ReadVariable()
        {
            // Reading from the local variable reads from the remote variable on the RC7 controller
            label_VariableContent.Text = RC7variable_S10.Value.ToString();
        }

        private void button_WriteVariable_Click(object sender, EventArgs e)
        {
            // Write value of text box to S10 variable.
            RC7variable_S10.Value = textBox_WriteVar.Text.ToString();
        }

        private void button_Close_Click(object sender, EventArgs e)
        {
            // Properly shut down the local variables in reverse order of precedence so that the ORiN engine is not left hanging
            RC7Controller.Variables.Clear();	// Clear all local variables from the controller variable (THIS DOES NOT AFFECT VALUES ON THE PHYSICAL RC7 CONTROLLER)
            RC7variable_S10 = null;				// Set the individual local variables to null

            RC7Task_RobSlave.Stop(1,"");        //Stop the RobSlave program. (this is just a suggestion)
            RC7Controller.Tasks.Clear();        // Clear all local tasks handles from the controller variable
            RC7Task_RobSlave = null;            // Set the individual local variables to null

            ORiN_Engine.Workspaces.Item(0).Controllers.Remove(RC7Controller.Index);	// Remove the controller variable from the ORiN engine
            RC7Controller = null;				// Set the individual controller variable to null

            ORiN_Engine = null;					// Set the ORiN engine variable to null

            Close();							// Exit this C# program
        }

    }
}
