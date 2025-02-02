import os
import cv2 as cv
import numpy as np
import matplotlib.pyplot as plt
import random
from utils import change_thickness, resize_images, crop_128


for j in range(1, 63):
    perc = j / 62 * 100
    perc = round(perc, 2)
    if(j < 10):
        j = f"0{j}"
    change_thickness(j)
    resize_images(j)
    print(f"{j}/62, {perc}% concluded.")
