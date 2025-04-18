import Attack from "./Attack";
import Health from "./Health";
import Tier from "./Tier";
import Cost from "./Cost";
import { cn, delay } from "../util";
import Damage from "./Damage";
import { useState } from "react";
import useAsyncEffect from "use-async-effect";
import Tooltip from "./Tooltip";
import DivineShield from "./DivineShield";
import Venomous from "./Venomous";
import Taunt from "./Taunt";
import Stealth from "./Stealth";
import Reborn from "./Reborn";
import Windfury from "./Windfury";
import Paralyze from "./Paralyze";

export default function Pokemon({
  attack,
  health,
  name,
  tier,
  id,
  cardType,
  cost,
  num,
  showTier = false,
  className,
  style,
  damage,
  damageType,
  text,
  keywords,
  baseHealth,
  baseAttack,
  type,
  location,
  isFrozen,
  isTemporary,
}) {
  const isMinion = cardType === 0;
  const [showDamage, setShowDamage] = useState(false);
  const [minionStatus, setMinionStatus] = useState("alive");
  const [showToolTip, setShowToolTip] = useState(false);

  useAsyncEffect(async () => {
    setShowDamage(true);
    await delay(1500);
    setShowDamage(false);
  }, [damage]);

  useAsyncEffect(async () => {
    if (health <= 0 && isMinion) {
      await delay(750);
      setMinionStatus("dying");
      await delay(750);
      setMinionStatus("dead");
    }
  }, [health]);

  if (minionStatus === "dead") {
    return;
  }

  const pokemon = (
    <div className="mx-px h-20 w-20">
      <div
        id={id}
        style={{
          backgroundImage: `url(/pokemon/${num}.png)`,
          ...style,
        }}
        className={cn(
          "flex h-20 w-20 items-center justify-center rounded-xl transition-all duration-200 ease-in-out",
          isMinion && `bg-contain bg-center`,
          !isMinion && "border-2 border-red-900 bg-red-300",
          minionStatus === "dying" && "opacity-0 duration-750",
          isFrozen && "outline-5 outline-blue-100",
          isFrozen && isMinion && "bg-blue-100",
          isTemporary && "border-red-800 bg-red-200 text-gray-800",
          className,
        )}
        onMouseEnter={() => setShowToolTip(true)}
        onMouseLeave={() => setShowToolTip(false)}
      >
        <div className="relative flex h-20 w-20 flex-col items-center justify-center">
          {!isMinion && (
            <span className="w-20 px-1 text-center text-xs break-words">
              {name}
            </span>
          )}
          {isMinion && (
            <Attack
              attack={attack}
              shock={keywords.shock}
              burning={keywords.burning}
              isBuffed={attack > baseAttack}
            />
          )}
          {isMinion && (
            <Health
              health={health}
              animate={health < baseHealth}
              isBuffed={health > baseHealth}
            />
          )}
          {!!tier && showTier && <Tier tier={tier} />}
          {!isMinion && Number.isInteger(cost) && location !== "hand" && (
            <Cost cost={cost} />
          )}
          {!!damage && showDamage && (
            <Damage damage={damage} damageType={damageType} />
          )}
          {keywords.divineShield && <DivineShield />}
          {keywords.venomous && <Venomous />}
          {keywords.taunt && <Taunt />}
          {keywords.stealth && <Stealth />}
          {keywords.reborn && <Reborn />}
          {keywords.windfury && <Windfury />}
          {keywords.paralyzed && <Paralyze />}
        </div>
      </div>
    </div>
  );

  return (
    <Tooltip
      cardType={cardType}
      name={name}
      tier={tier}
      showToolTip={showToolTip}
      text={text}
      types={type}
      location={location}
      positions={location === "board" ? ["bottom"] : ["top"]}
    >
      {pokemon}
    </Tooltip>
  );
}
