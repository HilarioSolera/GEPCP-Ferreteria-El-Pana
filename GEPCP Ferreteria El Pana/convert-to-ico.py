#!/usr/bin/env python3
"""
Convertir logo-el-pana.jpg a GEPCP.ico
Requiere: pip install Pillow
"""

import os
import sys
from pathlib import Path

try:
	from PIL import Image
except ImportError:
	print("[ERROR] Pillow no está instalado.")
	print("Ejecutar: pip install Pillow")
	sys.exit(1)

def convert_to_ico():
	logo_path = Path("wwwroot/images/logo-el-pana.jpg")
	ico_path = Path("GEPCP.ico")

	if not logo_path.exists():
		print(f"[ERROR] No se encontró: {logo_path}")
		return False

	print(f"Convertiendo {logo_path} a {ico_path}...")

	try:
		# Abrir imagen
		img = Image.open(logo_path)

		# Convertir a RGB si es necesario
		if img.mode in ('RGBA', 'LA', 'P'):
			background = Image.new('RGB', img.size, (255, 255, 255))
			background.paste(img, mask=img.split()[-1] if img.mode == 'RGBA' else None)
			img = background

		# Crear múltiples resoluciones para el ICO
		sizes = [(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
		icon_images = []

		for size in sizes:
			resized = img.resize(size, Image.Resampling.LANCZOS)
			icon_images.append(resized)

		# Guardar como ICO
		icon_images[0].save(
			ico_path,
			format='ICO',
			sizes=[img.size for img in icon_images]
		)

		print(f"[OK] Icono generado correctamente: {ico_path}")
		return True

	except Exception as e:
		print(f"[ERROR] {str(e)}")
		return False

if __name__ == "__main__":
	if convert_to_ico():
		sys.exit(0)
	else:
		sys.exit(1)
