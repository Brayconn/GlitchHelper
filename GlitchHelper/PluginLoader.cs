using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GlitchHelper
{
    public static class PluginLoader<T>
    {
        //Code taken from https://code.msdn.microsoft.com/windowsdesktop/Creating-a-simple-plugin-b6174b62
        public static ICollection<T> LoadPlugins(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    string[] dlls = Directory.GetFiles(path, "*.dll");

                    ICollection<Assembly> assemblies = new List<Assembly>(dlls.Length);

                    for (int i = 0; i < dlls.Length; i++)
                    {
                        //MessageBox.Show($"Loading {dlls[i]}");
                        assemblies.Add(Assembly.Load(AssemblyName.GetAssemblyName(dlls[i]))); //This is usually where FileLoadExceptions happen. (See catch for most common reason)
                        /*
                        AssemblyName an = AssemblyName.GetAssemblyName(dlls[i]);
                        MessageBox.Show("AssemblyName gotten");
                        Assembly assembly = Assembly.Load(an);
                        MessageBox.Show("Assembly Loaded");
                        assemblies.Add(assembly);
                        MessageBox.Show("Assembly added");
                        //*/
                        
                    }

                    Type pluginType = typeof(T);
                    ICollection<Type> pluginTypes = new List<Type>();
                    for (int i = 0; i < assemblies.Count; i++)
                    {
                        if (assemblies.ElementAt(i) != null)
                        {
                            Type[] types = assemblies.ElementAt(i).GetTypes();
                            for (int _i = 0; _i < types.Length; _i++)
                            {
                                if (!types[_i].IsInterface && !types[_i].IsAbstract && types[_i].GetInterface(pluginType.FullName) != null)
                                {
                                    pluginTypes.Add(types[_i]);
                                }

                                /*
                                if (types[_i].IsInterface || types[_i].IsAbstract)
                                {
                                    continue;
                                }
                                else
                                {
                                    if (types[_i].GetInterface(pluginType.FullName) != null)
                                    {
                                        pluginTypes.Add(types[_i]);
                                    }
                                }
                                */
                            }
                        }
                    }

                    ICollection<T> plugins = new List<T>(pluginTypes.Count);
                    for (int i = 0; i < pluginTypes.Count; i++)
                    {
                        plugins.Add((T)Activator.CreateInstance(pluginTypes.ElementAt(i)));
                        /*
                        T plugin = (T)Activator.CreateInstance(pluginTypes.ElementAt(i));
                        plugins.Add(plugin);
                        */
                    }

                    return plugins;
                }
            }
            catch(FileLoadException e)
            {
                //TODO maybe put this on a launch option?
                /* Debugging stuff
                string[] kvp = new string[e.Data.Count];
                string[] keys = e.Data.Keys.Cast<string>().ToArray();
                string[] values = e.Data.Values.Cast<string>().ToArray();
                for (int i = 0; i < kvp.Length; i++)
                {
                    kvp[i] = keys[i] + "\t" + values[i];
                }

                File.WriteAllLines(Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), @"AnnoyingErrorLog.txt"), new string[]
                    {
                        e.FileName,
                        e.Message,
                        e.HResult.ToString(),
                        e.Source,
                        e.StackTrace,
                    }.Concat(kvp));
                */
                MessageBox.Show("Oh great, this bug again...\nTry selecting each .dll included with GlitchHelper, right click -> properties -> \"Unblock\".\nIf this doesn't solve the issue, please contact /u/Brayconn."); //TODO Stop using Messagebox.Show
            }
            return null;
        }
    }
}
