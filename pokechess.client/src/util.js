import clsx from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs) {
  return twMerge(clsx(inputs));
}

export function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export function getElementPosition(id) {
  const element = document?.getElementById(id);
  const rect = element?.getBoundingClientRect();

  if (!rect) {
    throw new Error("Could not find ID:", id);
  }

  return [rect?.top, rect?.left];
}

export function hideElement(id) {
  const element = document.getElementById(id);
  element.style.display = "none";
}

export function showElement(id) {
  const element = document.getElementById(id);
  element.style.display = "block";
}

export function moveElement(id, [top, left]) {
  const element = document.getElementById(id);
  element.style.postion = "fixed";
  element.style.top = top;
  element.style.left = left;
}

export function returnElement(id) {
  const element = document.getElementById(id);
  element.style.postion = "relative";
  element.style.top = "";
  element.style.left = "";
}
