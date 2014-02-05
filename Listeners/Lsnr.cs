/***************************************************************************
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) version 3.                                           *
 *                                                                         *
 *   This program is distributed in the hope that it will be useful,       *
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 *   GNU General Public License for more details.                          *
 *                                                                         *
 *   You should have received a copy of the GNU General Public License     *
 *   along with this program; if not, write to the                         *
 *   Free Software Foundation, Inc.,                                       *
 *   51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.         *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;
using System.Threading;
using System.Text;

namespace pidgeon_sv
{
	/// <summary>
	/// Listener abstract class
	/// </summary>
	public class Lsnr
	{
        public int Port = 65534;

		/// <summary>
		/// Initializes a new instance of the <see cref="pidgeon_sv.Lsnr"/> class.
		/// </summary>
		public Lsnr ()
		{
		}

		public virtual bool Listen()
		{
			// by default we don't do anything end return false
			return false;
		}

		public virtual bool Close()
		{
			return false;
		}
	}
}

