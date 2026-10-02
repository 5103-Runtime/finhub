// Contributor: Marlon Gonzalez
import { Route, Routes} from 'react-router-dom'
import Register from './register/Register'

function App() {
  return <Routes>
    {/* TODO: Change default route when its page is created */}
     <Route path = "/" element={<Register/>}></Route>
  </Routes>
}

export default App
