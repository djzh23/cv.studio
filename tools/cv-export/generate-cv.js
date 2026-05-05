#!/usr/bin/env node
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import JSZip from "jszip";
import PDFDocument from "pdfkit";
import {
  AlignmentType,
  BorderStyle,
  Document,
  Footer,
  HeadingLevel,
  ImageRun,
  LevelFormat,
  Packer,
  Paragraph,
  ShadingType,
  TabStopPosition,
  TabStopType,
  Table,
  TableCell,
  TableLayoutType,
  TableRow,
  TextRun,
  WidthType,
} from "docx";
import { cvData } from "./cv-data.js";

const COLORS = {
  NAVY: "1A3A5C",
  TEAL: "1A7A6E",
  SILVER: "8A9BB0",
  SECTION_BG: "F2F6FA",
  RULE: "C5D5E8",
  BODY: "1C2833",
};

const PAGE = {
  WIDTH: 11906,
  HEIGHT: 16838,
  MARGIN: 720,
  CW: 10466,
  HEADER_COL_1: 1300,
  HEADER_COL_2: 9166,
};

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const outDir = path.join(__dirname, "out");
const photoPath = path.join(outDir, "profile-circle.png");
const docxPath = path.join(outDir, "cv_ats.docx");
const pdfPath = path.join(outDir, "cv_ats.pdf");

function sectionHeader(text) {
  const headerText = `  ${text.toUpperCase()}`;

  return [
    new Paragraph({
      spacing: { before: 120, after: 0 },
      children: [new TextRun(" ")],
    }),
    new Paragraph({
      spacing: { before: 0, after: 100 },
      shading: {
        type: ShadingType.CLEAR,
        fill: COLORS.SECTION_BG,
      },
      border: {
        top: { style: BorderStyle.NONE, size: 0, color: COLORS.RULE },
        left: { style: BorderStyle.SINGLE, size: 18, color: COLORS.NAVY },
        bottom: { style: BorderStyle.SINGLE, size: 2, color: COLORS.RULE },
        right: { style: BorderStyle.NONE, size: 0, color: COLORS.RULE },
      },
      children: [
        new TextRun({
          text: headerText,
          bold: true,
          size: 44,
          font: "Calibri",
          color: COLORS.NAVY,
        }),
      ],
    }),
  ];
}

function experienceEntry(item) {
  const lineAChildren = [
    new TextRun({ text: item.company, bold: true, size: 42, font: "Calibri", color: COLORS.BODY }),
  ];

  if (item.location) {
    lineAChildren.push(new TextRun({ text: ` | ${item.location}`, size: 38, color: COLORS.SILVER, font: "Calibri" }));
  }

  lineAChildren.push(new TextRun({ text: "\t" }));
  lineAChildren.push(new TextRun({ text: item.period, italics: true, size: 38, color: COLORS.SILVER, font: "Calibri" }));

  const paragraphs = [
    new Paragraph({
      tabStops: [{ type: TabStopType.RIGHT, position: TabStopPosition.MAX }],
      children: lineAChildren,
    }),
    new Paragraph({
      spacing: { after: 60 },
      children: [
        new TextRun({ text: item.role, size: 40, bold: true, italics: true, color: COLORS.TEAL, font: "Calibri" }),
      ],
    }),
  ];

  item.bullets.forEach((bullet) => {
    paragraphs.push(
      new Paragraph({
        spacing: { after: 40 },
        numbering: { reference: "cv-bullets", level: 0 },
        children: [
          new TextRun({ text: bullet, size: 38, color: COLORS.BODY, font: "Calibri" }),
        ],
      }),
    );
  });

  return paragraphs;
}

function educationEntry(item) {
  return [
    new Paragraph({
      tabStops: [{ type: TabStopType.RIGHT, position: TabStopPosition.MAX }],
      children: [
        new TextRun({ text: item.school, bold: true, size: 42, color: COLORS.BODY, font: "Calibri" }),
        ...(item.city ? [new TextRun({ text: ` | ${item.city}`, size: 38, color: COLORS.SILVER, font: "Calibri" })] : []),
        new TextRun({ text: "\t" }),
        new TextRun({ text: item.period, size: 38, color: COLORS.SILVER, italics: true, font: "Calibri" }),
      ],
    }),
    new Paragraph({
      spacing: { after: 80 },
      children: [new TextRun({ text: item.degree, size: 40, italics: true, color: COLORS.TEAL, font: "Calibri" })],
    }),
  ];
}

function skillsParagraphs() {
  return cvData.skills.map((s) =>
    new Paragraph({
      spacing: { after: 40 },
      children: [
        new TextRun({ text: `${s.category}:  `, bold: true, size: 38, color: COLORS.NAVY, font: "Calibri" }),
        new TextRun({ text: s.items.join(", "), size: 38, color: COLORS.BODY, font: "Calibri" }),
      ],
    }),
  );
}

function projectsParagraphs() {
  const paras = [];

  cvData.projects.forEach((p) => {
    paras.push(
      new Paragraph({
        spacing: { after: 40 },
        children: [
          new TextRun({ text: p.name, bold: true, size: 40, color: COLORS.BODY, font: "Calibri" }),
          new TextRun({ text: ` (${p.stack})`, size: 38, color: COLORS.SILVER, font: "Calibri" }),
        ],
      }),
    );

    p.bullets.forEach((bullet) => {
      paras.push(
        new Paragraph({
          spacing: { after: 40 },
          numbering: { reference: "cv-bullets", level: 0 },
          children: [new TextRun({ text: bullet, size: 38, color: COLORS.BODY, font: "Calibri" })],
        }),
      );
    });
  });

  return paras;
}

async function buildDocx() {
  const photoBytes = fs.existsSync(photoPath) ? fs.readFileSync(photoPath) : null;

  const headerRightCellParagraphs = [
    new Paragraph({
      children: [new TextRun({ text: cvData.name, bold: true, size: 104, color: COLORS.NAVY, font: "Calibri" })],
    }),
    new Paragraph({
      children: [new TextRun({ text: cvData.title, italics: true, size: 48, color: COLORS.TEAL, font: "Calibri" })],
    }),
    new Paragraph({
      alignment: AlignmentType.CENTER,
      children: [
        new TextRun({ text: cvData.contact.email, size: 38, color: COLORS.SILVER, font: "Calibri" }),
        new TextRun({ text: " · ", size: 38, color: COLORS.SILVER, font: "Calibri" }),
        new TextRun({ text: cvData.contact.phone, size: 38, color: COLORS.SILVER, font: "Calibri" }),
        new TextRun({ text: " · ", size: 38, color: COLORS.SILVER, font: "Calibri" }),
        new TextRun({ text: cvData.contact.city, size: 38, color: COLORS.SILVER, font: "Calibri" }),
        new TextRun({ text: " · ", size: 38, color: COLORS.SILVER, font: "Calibri" }),
        new TextRun({ text: cvData.contact.linkedin, size: 38, color: COLORS.SILVER, font: "Calibri" }),
      ],
    }),
  ];

  const doc = new Document({
    numbering: {
      config: [
        {
          reference: "cv-bullets",
          levels: [
            {
              level: 0,
              format: LevelFormat.BULLET,
              text: "•",
              alignment: AlignmentType.LEFT,
              style: {
                paragraph: {
                  indent: { left: 400, hanging: 240 },
                },
              },
            },
          ],
        },
      ],
    },
    sections: [
      {
        properties: {
          page: {
            size: { width: PAGE.WIDTH, height: PAGE.HEIGHT },
            margin: { top: PAGE.MARGIN, right: PAGE.MARGIN, bottom: PAGE.MARGIN, left: PAGE.MARGIN },
          },
        },
        children: [
          new Table({
            width: { size: PAGE.CW, type: WidthType.DXA },
            layout: TableLayoutType.FIXED,
            rows: [
              new TableRow({
                children: [
                  new TableCell({
                    width: { size: PAGE.HEADER_COL_1, type: WidthType.DXA },
                    borders: {
                      top: { style: BorderStyle.NONE, size: 0, color: "FFFFFF" },
                      bottom: { style: BorderStyle.NONE, size: 0, color: "FFFFFF" },
                      left: { style: BorderStyle.NONE, size: 0, color: "FFFFFF" },
                      right: { style: BorderStyle.NONE, size: 0, color: "FFFFFF" },
                    },
                    children: [
                      new Paragraph({
                        alignment: AlignmentType.CENTER,
                        children: photoBytes
                          ? [
                              new ImageRun({
                                data: photoBytes,
                                transformation: { width: 90, height: 90 },
                                type: "png",
                              }),
                            ]
                          : [new TextRun({ text: "" })],
                      }),
                    ],
                  }),
                  new TableCell({
                    width: { size: PAGE.HEADER_COL_2, type: WidthType.DXA },
                    margins: { left: 240, right: 0, top: 0, bottom: 0 },
                    borders: {
                      top: { style: BorderStyle.NONE, size: 0, color: "FFFFFF" },
                      bottom: { style: BorderStyle.NONE, size: 0, color: "FFFFFF" },
                      left: { style: BorderStyle.SINGLE, size: 14, color: COLORS.NAVY },
                      right: { style: BorderStyle.NONE, size: 0, color: "FFFFFF" },
                    },
                    children: headerRightCellParagraphs,
                  }),
                ],
              }),
            ],
          }),
          new Paragraph({
            border: {
              bottom: { style: BorderStyle.SINGLE, size: 10, color: COLORS.NAVY },
            },
            children: [new TextRun(" ")],
            spacing: { after: 80 },
          }),

          ...sectionHeader("Qualifikationsprofil"),
          new Paragraph({
            spacing: { after: 80 },
            children: [new TextRun({ text: cvData.summary, size: 38, color: COLORS.BODY, font: "Calibri" })],
          }),

          ...sectionHeader("Kenntnisse"),
          ...skillsParagraphs(),

          ...sectionHeader("Berufserfahrung"),
          ...cvData.experience.flatMap(experienceEntry),

          ...sectionHeader("Ausbildung"),
          ...cvData.education.flatMap(educationEntry),

          ...sectionHeader("Projekte"),
          ...projectsParagraphs(),

          ...sectionHeader("Sprachen & Interessen"),
          new Paragraph({
            children: [
              new TextRun({ text: "Sprachen: ", bold: true, size: 38, color: COLORS.NAVY, font: "Calibri" }),
              new TextRun({ text: cvData.languagesInterests.languages, size: 38, color: COLORS.BODY, font: "Calibri" }),
              new TextRun({ text: " | ", size: 38, color: COLORS.SILVER, font: "Calibri" }),
              new TextRun({ text: "Interessen: ", bold: true, size: 38, color: COLORS.NAVY, font: "Calibri" }),
              new TextRun({ text: cvData.languagesInterests.interests, size: 38, color: COLORS.BODY, font: "Calibri" }),
            ],
          }),
        ],
        footers: {
          default: new Footer({ children: [new Paragraph({ children: [new TextRun("")] })] }),
        },
      },
    ],
  });

  const buffer = await Packer.toBuffer(doc);
  const fixed = await reorderPBdr(buffer);
  fs.writeFileSync(docxPath, fixed);
}

async function reorderPBdr(docxBuffer) {
  const zip = await JSZip.loadAsync(docxBuffer);
  const xml = await zip.file("word/document.xml").async("string");

  const fixedXml = xml.replace(/<w:pBdr>([\s\S]*?)<\/w:pBdr>/g, (_, inner) => {
    const top = inner.match(/<w:top\b[^/>]*\/>/)?.[0] ?? "";
    const left = inner.match(/<w:left\b[^/>]*\/>/)?.[0] ?? "";
    const bottom = inner.match(/<w:bottom\b[^/>]*\/>/)?.[0] ?? "";
    const right = inner.match(/<w:right\b[^/>]*\/>/)?.[0] ?? "";

    const rest = inner
      .replace(/<w:top\b[^/>]*\/>/g, "")
      .replace(/<w:left\b[^/>]*\/>/g, "")
      .replace(/<w:bottom\b[^/>]*\/>/g, "")
      .replace(/<w:right\b[^/>]*\/>/g, "")
      .trim();

    return `<w:pBdr>${top}${left}${bottom}${right}${rest}</w:pBdr>`;
  });

  zip.file("word/document.xml", fixedXml);
  return zip.generateAsync({ type: "nodebuffer" });
}

function buildPdf() {
  const doc = new PDFDocument({
    size: "A4",
    margins: { top: 62.36, right: 62.36, bottom: 62.36, left: 62.36 },
  });

  const stream = fs.createWriteStream(pdfPath);
  doc.pipe(stream);

  const navy = "#1A3A5C";
  const teal = "#1A7A6E";
  const silver = "#8A9BB0";
  const body = "#1C2833";
  const sectionBg = "#F2F6FA";
  const rule = "#C5D5E8";

  const leftX = doc.page.margins.left;
  const topY = doc.page.margins.top;
  const contentW = doc.page.width - doc.page.margins.left - doc.page.margins.right;

  const col1 = (1300 / 20);
  const col2 = (9166 / 20);
  const rowH = 110;

  if (fs.existsSync(photoPath)) {
    doc.save();
    doc.circle(leftX + 45, topY + 45, 45).clip();
    doc.image(photoPath, leftX, topY, { width: 90, height: 90 });
    doc.restore();
  }

  doc
    .moveTo(leftX + col1 + 12, topY)
    .lineTo(leftX + col1 + 12, topY + rowH)
    .lineWidth(1)
    .strokeColor(navy)
    .stroke();

  const txtX = leftX + col1 + 24;
  doc.fillColor(navy).font("Helvetica-Bold").fontSize(26).text(cvData.name, txtX, topY + 2, { width: col2 - 24 });
  doc.fillColor(teal).font("Helvetica-Oblique").fontSize(12).text(cvData.title, txtX, topY + 40, { width: col2 - 24 });

  doc
    .fillColor(silver)
    .font("Helvetica")
    .fontSize(10.5)
    .text(
      `${cvData.contact.email} · ${cvData.contact.phone} · ${cvData.contact.city} · ${cvData.contact.linkedin}`,
      txtX,
      topY + 64,
      { width: col2 - 24, align: "center" },
    );

  let y = topY + rowH + 10;
  doc.moveTo(leftX, y).lineTo(leftX + contentW, y).lineWidth(2).strokeColor(navy).stroke();
  y += 10;

  const renderSection = (title, bodyCb) => {
    y += 6;
    doc.rect(leftX, y, contentW, 20).fill(sectionBg);
    doc
      .lineWidth(0.5)
      .strokeColor(rule)
      .moveTo(leftX, y + 20)
      .lineTo(leftX + contentW, y + 20)
      .stroke();
    doc
      .lineWidth(1)
      .strokeColor(navy)
      .moveTo(leftX, y)
      .lineTo(leftX, y + 20)
      .stroke();

    doc.fillColor(navy).font("Helvetica-Bold").fontSize(11).text(`  ${title.toUpperCase()}`, leftX + 2, y + 4);
    y += 26;
    bodyCb();
    y += 8;
  };

  const ensure = (needed = 80) => {
    if (y + needed > doc.page.height - doc.page.margins.bottom) {
      doc.addPage();
      y = doc.page.margins.top;
    }
  };

  renderSection("Qualifikationsprofil", () => {
    ensure(70);
    doc.fillColor(body).font("Helvetica").fontSize(11).text(cvData.summary, leftX, y, { width: contentW, align: "left" });
    y = doc.y;
  });

  renderSection("Kenntnisse", () => {
    cvData.skills.forEach((s) => {
      ensure(30);
      doc.font("Helvetica-Bold").fontSize(10.5).fillColor(navy).text(`${s.category}: `, leftX, y, { continued: true });
      doc.font("Helvetica").fontSize(10.5).fillColor(body).text(s.items.join(", "));
      y = doc.y + 2;
    });
  });

  renderSection("Berufserfahrung", () => {
    cvData.experience.forEach((e) => {
      ensure(90);
      doc.font("Helvetica-Bold").fontSize(10.5).fillColor(body).text(`${e.company} | ${e.location}`, leftX, y, {
        width: contentW - 130,
        continued: true,
      });
      doc.font("Helvetica-Oblique").fontSize(9.5).fillColor(silver).text(e.period, leftX + contentW - 130, y, {
        width: 130,
        align: "right",
      });
      y = doc.y + 1;
      doc.font("Helvetica-Oblique").fontSize(10).fillColor(teal).text(e.role, leftX, y);
      y = doc.y + 2;

      e.bullets.forEach((b) => {
        ensure(28);
        doc.font("Helvetica").fontSize(10.5).fillColor(body).text(`• ${b}`, leftX + 8, y, {
          width: contentW - 8,
          lineGap: 1,
        });
        y = doc.y + 1;
      });
      y += 4;
    });
  });

  renderSection("Ausbildung", () => {
    cvData.education.forEach((e) => {
      ensure(55);
      doc.font("Helvetica-Bold").fontSize(10.5).fillColor(body).text(`${e.school} | ${e.city}`, leftX, y, {
        width: contentW - 130,
        continued: true,
      });
      doc.font("Helvetica-Oblique").fontSize(9.5).fillColor(silver).text(e.period, leftX + contentW - 130, y, {
        width: 130,
        align: "right",
      });
      y = doc.y + 1;
      doc.font("Helvetica-Oblique").fontSize(10).fillColor(teal).text(e.degree, leftX, y);
      y = doc.y + 5;
    });
  });

  renderSection("Projekte", () => {
    cvData.projects.forEach((p) => {
      ensure(55);
      doc.font("Helvetica-Bold").fontSize(10.5).fillColor(body).text(`${p.name} `, leftX, y, { continued: true });
      doc.font("Helvetica").fontSize(9.5).fillColor(silver).text(`(${p.stack})`);
      y = doc.y + 1;
      p.bullets.forEach((b) => {
        ensure(26);
        doc.font("Helvetica").fontSize(10.5).fillColor(body).text(`• ${b}`, leftX + 8, y, { width: contentW - 8 });
        y = doc.y + 1;
      });
      y += 4;
    });
  });

  renderSection("Sprachen & Interessen", () => {
    ensure(35);
    doc.font("Helvetica-Bold").fontSize(10.5).fillColor(navy).text("Sprachen: ", leftX, y, { continued: true });
    doc.font("Helvetica").fontSize(10.5).fillColor(body).text(cvData.languagesInterests.languages, { continued: true });
    doc.font("Helvetica").fontSize(10.5).fillColor(silver).text(" | ", { continued: true });
    doc.font("Helvetica-Bold").fontSize(10.5).fillColor(navy).text("Interessen: ", { continued: true });
    doc.font("Helvetica").fontSize(10.5).fillColor(body).text(cvData.languagesInterests.interests);
    y = doc.y;
  });

  doc.end();

  return new Promise((resolve, reject) => {
    stream.on("finish", resolve);
    stream.on("error", reject);
  });
}

async function main() {
  if (!fs.existsSync(outDir)) {
    fs.mkdirSync(outDir, { recursive: true });
  }

  await buildDocx();
  await buildPdf();

  const docxSize = fs.statSync(docxPath).size;
  const pdfSize = fs.statSync(pdfPath).size;

  if (docxSize <= 0 || pdfSize <= 0) {
    throw new Error("Output generation failed: empty file produced.");
  }

  console.log(`DOCX created: ${docxPath}`);
  console.log(`PDF created:  ${pdfPath}`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});


