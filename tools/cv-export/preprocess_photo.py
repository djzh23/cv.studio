from PIL import Image, ImageDraw
import argparse
import os


def preprocess(input_path: str, output_path: str):
    if not os.path.exists(input_path):
        raise FileNotFoundError(f"Input image not found: {input_path}")

    img = Image.open(input_path).convert("RGBA")
    w, h = img.size
    side = min(w, h)

    left = (w - side) // 2
    top = (h - side) // 2
    right = left + side
    bottom = top + side

    cropped = img.crop((left, top, right, bottom)).resize((300, 300), Image.Resampling.LANCZOS)

    mask = Image.new("L", (300, 300), 0)
    draw = ImageDraw.Draw(mask)
    draw.ellipse((0, 0, 300, 300), fill=255)

    out = Image.new("RGBA", (300, 300), (255, 255, 255, 0))
    out.paste(cropped, (0, 0), mask)

    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    out.save(output_path, format="PNG")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Preprocess profile photo to circular PNG.")
    parser.add_argument("--input", required=True, help="Path to input image")
    parser.add_argument("--output", default="tools/cv-export/out/profile-circle.png", help="Path to output PNG")
    args = parser.parse_args()

    preprocess(args.input, args.output)
    print(f"Saved: {args.output}")
