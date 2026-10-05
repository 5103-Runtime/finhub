// Contributor: Marlon Gonzalez
import { Route, Routes} from 'react-router-dom'
import Register from '@app/components/register/Register'
import Login from '@app/components/login/Login'

function App() {
  return <Routes>
    {/* TODO: Change default route when its page is created */}
     <Route path = "/" element={<Login/>}></Route>
     <Route path = "/login" element={<Login/>}></Route>
     <Route path='/register' element={<Register/>}></Route>
  </Routes>
}

export default App
