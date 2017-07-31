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
            if (Directory.Exists(path))
            {
                string[] dlls = Directory.GetFiles(path, "*.dll");

                ICollection<Assembly> assemblies = new List<Assembly>(dlls.Length);

                for (int i = 0; i < dlls.Length; i++)
                {
                    //MessageBox.Show($"Loading {dlls[i]}");
                    assemblies.Add(Assembly.Load(AssemblyName.GetAssemblyName(dlls[i])));
                    //MessageBox.Show("Loaded");
                    /*
                    AssemblyName an = AssemblyName.GetAssemblyName(dlls[i]);
                    Assembly assembly = Assembly.Load(an);
                    assemblies.Add(assembly);
                    */
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
            return null;
        }        
    }
}
