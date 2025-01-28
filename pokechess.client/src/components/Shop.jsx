import Hero from "./Hero";
import { Droppable } from "@hello-pangea/dnd";

export default function Shop({ player, isDropDisabled }) {
  function getListStyle(isDraggingOver) {
    return {
      // background: isDraggingOver ? "lightblue" : "lightgrey",
      // display: "flex",
      // padding: grid,
      // width: itemsLength * 68.44 + 16,
    };
  }

  return (
    <Droppable
      droppableId="droppable-sell"
      direction="horizontal"
      isDropDisabled={isDropDisabled}
    >
      {(provided, snapshot) => (
        <>
          <div
            ref={provided.innerRef}
            style={getListStyle(snapshot.isDraggingOver)}
            {...provided.droppableProps}
          >
            <Hero name="Shop" tier={player.tier} />
          </div>
          <span className="hidden">{provided.placeholder}</span>
        </>
      )}
    </Droppable>
  );
}
