from rdkit import Chem
from rdkit.Chem import rdDetermineBonds
from rdkit.Chem.Draw import rdMolDraw2D, rdDepictor
import numpy as np
from xml.dom import minidom

class StructureFormulaGenerator:

    # def __init__(self):
    #     pass

    # Function to extract 2D coordinates of atoms from an RDKit molecule
    def __get_atom_coordinates(self, mol):
        # Compute 2D coordinates
        rdDepictor.Compute2DCoords(mol)
        # Get atom coordinates
        conf = mol.GetConformer()
        atom_coords = [tuple(conf.GetAtomPosition(i))[:2] for i in range(mol.GetNumAtoms())]  # Extract (x, y) coordinates
        return atom_coords
    

    def get_structure_formula(self, atom_positions, symbols):
        assert(len(atom_positions) == len(symbols))

        atom_positions = np.array(atom_positions)

        xyz = f"{len(atom_positions)}\n\n"
        for i in range(len(atom_positions)):
            xyz += f"{symbols[i]}\t{atom_positions[i,0]}\t{atom_positions[i,1]}\t{atom_positions[i,2]}\n"

        rdDepictor.SetPreferCoordGen(True)

        raw_mol = Chem.MolFromXYZBlock(xyz)
        mol = Chem.Mol(raw_mol)
        rdDetermineBonds.DetermineBonds(mol,charge=0)
        #mol = Chem.AddHs(mol)
        d = rdMolDraw2D.MolDraw2DSVG(-1, -1)
        #rdMolDraw2D.SetDarkMode(d)
        d.drawOptions().padding = 0.0
        d.drawOptions().scalingFactor = 30
        rdMolDraw2D.PrepareAndDrawMolecule(d, mol)
        d.FinishDrawing()
        svg_content = d.GetDrawingText()

        # svg_file = 'test.svg'
        # with open(svg_file, 'w') as f:
        #     f.write(svg_content)

        coords_on_svg = self.__calc_2d_atom_positions(mol, svg_content)
        #new_svg_content = add_circles_to_svg(svg_content, transformed_coords)

        return svg_content, coords_on_svg


    # def get_structure_formula(self, smiles = "CCC"):
    #     rdDepictor.SetPreferCoordGen(True)

    #     mol = Chem.MolFromSmiles(smiles)

    #     mol = Chem.AddHs(mol)
    #     d = rdMolDraw2D.MolDraw2DSVG(-1, -1)
    #     #rdMolDraw2D.SetDarkMode(d)
    #     d.drawOptions().padding = 0.0
    #     d.drawOptions().scalingFactor = 30
    #     rdMolDraw2D.PrepareAndDrawMolecule(d, mol)
    #     d.FinishDrawing()
    #     svg_content = d.GetDrawingText()

    #     # svg_file = 'test.svg'
    #     # with open(svg_file, 'w') as f:
    #     #     f.write(svg_content)

    #     coords_on_svg = self.__calc_2d_atom_positions(mol, svg_content)
    #     #new_svg_content = add_circles_to_svg(svg_content, transformed_coords)

    #     return svg_content, coords_on_svg


    def __calc_2d_atom_positions(self, mol, svg_content):
        # Get atom coordinates
        atom_coords = self.__get_atom_coordinates(mol)

        # Get SVG dimensions
        svg_width, svg_height = self.__get_svg_dimensions(svg_content)

        margin_x = 5#3.65 # 2,950
        margin_y = 6.5#4.35 # 4,350

        middle_x = svg_width / 2.0
        middle_y = svg_height / 2.0
        middle = np.array([middle_x, middle_y])

        # Transform coordinates to fit SVG and correct for size of symbols at the boarder (margin)
        transformed_coords = self.__transform_coordinates(atom_coords, svg_width - 2 * margin_x, svg_height - 2 * margin_y)

        transformed_coords = np.array(transformed_coords)
        for i in range(transformed_coords.shape[0]):
            transformed_coords[i] = transformed_coords[i] + np.array([margin_x, margin_y])

        return transformed_coords


    def __transform_coordinates(self, atom_coords, svg_width, svg_height):
        # Find minimum and maximum x and y coordinates
        min_x = min(coord[0] for coord in atom_coords)
        max_x = max(coord[0] for coord in atom_coords)
        min_y = min(coord[1] for coord in atom_coords)
        max_y = max(coord[1] for coord in atom_coords)
        
        # Calculate scaling factors
        scale_x = svg_width / (max_x - min_x)
        scale_y = svg_height / (max_y - min_y)
        
        # Translate and scale coordinates
        transformed_coords = []
        for coord in atom_coords:
            x = (coord[0] - min_x) * scale_x
            y = svg_height - (coord[1] - min_y) * scale_y
            transformed_coords.append((x, y))
        
        return transformed_coords


    def __get_svg_dimensions(self, svg_content):
        #svg_doc = minidom.parse(svg_file)
        svg_doc = minidom.parseString(svg_content)
        svg_element = svg_doc.getElementsByTagName('svg')[0]
        width = float(svg_element.getAttribute('width').replace('px', ''))
        height = float(svg_element.getAttribute('height').replace('px', ''))
        return width, height
    

    def __add_circles_to_svg(self, svg_content, transformed_coords, radius=2, fill='blue'):
        circle_template = '<circle cx="{0}" cy="{1}" r="{2}" fill="{3}" />'
        circles = [circle_template.format(coord[0], coord[1], radius, fill) for coord in transformed_coords]
        svg_content = svg_content.replace('</svg>', '\n'.join(circles) + '</svg>')
        return svg_content
