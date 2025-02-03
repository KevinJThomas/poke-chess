import { cn } from "../util";

const colors = {
  Normal: "#A8A77A",
  Fire: "#EE8130",
  Water: "#6390F0",
  Electric: "#F7D02C",
  Grass: "#7AC74C",
  Ice: "#96D9D6",
  Fighting: "#C22E28",
  Poison: "#A33EA1",
  Ground: "#E2BF65",
  Flying: "#A98FF3",
  Psychic: "#F95587",
  Bug: "#A6B91A",
  Rock: "#B6A136",
  Ghost: "#735797",
  Dragon: "#6F35FC",
  Dark: "#705746",
  Steel: "#B7B7CE",
  Fairy: "#D685AD",
};

export default function TypeTooltip({ types, className }) {
  return (
    <div className="flex">
      {types.map((type, index) => (
        <div
          key={type}
          style={{ backgroundColor: colors[type] }}
          className={cn(
            "p-1 text-xs text-black",
            index === 0 && "rounded-tl-md rounded-bl-md",
            index === types.length - 1 && "rounded-tr-md rounded-br-md",
            className,
          )}
        >
          {type}
        </div>
      ))}
    </div>
  );
}
